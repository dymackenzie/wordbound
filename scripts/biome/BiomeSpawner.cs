using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BiomeSpawner : Node
{
    [Signal] public delegate void WaveStartedEventHandler(int waveIndex);
    [Signal] public delegate void WaveFinishedEventHandler(int waveIndex);
    [Signal] public delegate void EnemySpawnedEventHandler(Node enemyInstance, string enemyId);

    [Export] public NodePath SpawnParentPath;
    [Export] public string BiomeFile = "res://data/biomes/glade.json";

    [Export] public float StaggerInterval = 0.5f;
    [Export] public float SpawnMarginMin = 64.0f;
    [Export] public float SpawnMarginMax = 256.0f;
    [Export] public bool SpawnOnReady = false;
    [Export] public int StartWave = 0;

    private BiomeDataReader.BiomeDefinition _biome = null;
    private Node _spawnParent = null;
    private RandomNumberGenerator _rng = new();
    
    private bool _isSpawning = false;

    public override void _Ready()
    {
        _rng.Randomize();
        _spawnParent = GetNodeOrNull(SpawnParentPath) ?? this;
        LoadBiome();
        if (SpawnOnReady)
            SpawnWave(StartWave);
    }

    public void SetBiome(string biomeFile)
    {
        BiomeFile = biomeFile;
        LoadBiome();
    }

    public void LoadBiome()
    {
        _biome = BiomeDataReader.LoadBiomeFromFile(BiomeFile);
    }

    /// <summary>
    /// Spawns enemies for the given wave index using the biome's budget rules.
    /// This method starts an async spawn routine and returns immediately.
    /// </summary>
    public void SpawnWave(int waveIndex)
    {
        if (_isSpawning)
        {
            GD.Print("LevelSpawner: already spawning a wave, ignoring request");
            return;
        }

        if (_biome == null)
            LoadBiome();

        // start async routine
        _ = SpawnWaveAsync(waveIndex);
    }

    private async Task SpawnWaveAsync(int waveIndex)
    {
        StartWaveHandler(waveIndex);

        int budget = GetWaveBudget(waveIndex);
        int minCost = ComputeMinCost();
        string mode = _biome.SpawnMode.ToLowerInvariant();

        GD.Print($"LevelSpawner: starting wave {waveIndex} (budget={budget}) spawn_mode={mode}");

        while (budget >= minCost)
        {
            var pick = PickEnemyForBudget(budget);
            if (pick == null)
                break;
            var spawned = SpawnEnemy(pick);
            EmitSignal(nameof(EnemySpawnedEventHandler), spawned, pick.Id);
            budget -= pick.Cost;

            if (budget >= minCost)
                await ToSignal(GetTree().CreateTimer(StaggerInterval), "timeout");
        }

        GD.Print($"LevelSpawner: finished wave {waveIndex}");

        StopWaveHandler(waveIndex);
    }

    private void StartWaveHandler(int waveIndex)
    {
        _isSpawning = true;
        EmitSignal(nameof(WaveStartedEventHandler), waveIndex);
    }
    
    private void StopWaveHandler(int waveIndex)
    {
        EmitSignal(nameof(WaveFinishedEventHandler), waveIndex);
        _isSpawning = false;
    }

    private BiomeDataReader.EnemyDefinition PickEnemyForBudget(int budget)
    {
        var candidates = BuildCandidatesForBudget(budget, out double totalWeight);
        if (candidates == null || candidates.Count == 0)
            return null;
        return ChooseFromCandidates(candidates, totalWeight);
    }

    private int GetWaveBudget(int waveIndex)
    {
        return _biome.BaseBudget + _biome.BudgetAdditionPerWave * waveIndex;
    }

    private int ComputeMinCost()
    {
        int minCost = int.MaxValue;
        foreach (var e in _biome.Pool)
            if (e != null && e.Cost < minCost)
                minCost = e.Cost;
        if (minCost == int.MaxValue || minCost <= 0)
            minCost = 1;
        return minCost;
    }

    private List<BiomeDataReader.EnemyDefinition> BuildCandidatesForBudget(int budget, out double totalWeight)
    {
        var candidates = new List<BiomeDataReader.EnemyDefinition>();
        totalWeight = 0.0;
        foreach (var e in _biome.Pool)
        {
            if (e == null)
                continue;
            if (e.Cost <= budget)
            {
                candidates.Add(e);
                totalWeight += Math.Max(0.0, e.Weight);
            }
        }
        return candidates;
    }

    private BiomeDataReader.EnemyDefinition ChooseFromCandidates(List<BiomeDataReader.EnemyDefinition> candidates, double totalWeight)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        if (totalWeight <= 0)
        {
            int idx = _rng.RandiRange(0, Math.Max(0, candidates.Count - 1));
            return candidates[idx];
        }

        double pick = _rng.Randf(); // 0..1
        double accum = 0.0;
        foreach (var c in candidates)
        {
            double w = Math.Max(0.0, c.Weight) / totalWeight;
            accum += w;
            if (pick <= accum)
                return c;
        }

        return candidates[^1];
    }

    private Vector2 GetSpawnPositionOutsideViewport()
    {
        var vp = GetViewport();
        var rect = vp.GetVisibleRect();
        float margin = _rng.RandfRange(SpawnMarginMin, SpawnMarginMax);
        int side = _rng.RandiRange(0, 3);
        return side switch
        {
            // left
            0 => new Vector2(rect.Position.X - margin, _rng.RandfRange(rect.Position.Y, rect.Position.Y + rect.Size.Y)),
            // right
            1 => new Vector2(rect.Position.X + rect.Size.X + margin, _rng.RandfRange(rect.Position.Y, rect.Position.Y + rect.Size.Y)),
            // top
            2 => new Vector2(_rng.RandfRange(rect.Position.X, rect.Position.X + rect.Size.X), rect.Position.Y - margin),
            // bottom
            _ => new Vector2(_rng.RandfRange(rect.Position.X, rect.Position.X + rect.Size.X), rect.Position.Y + rect.Size.Y + margin),
        };
    }

    /// <summary>
    /// Stop any currently running spawn routine. The async loop checks _isSpawning
    /// and will exit early.
    /// </summary>
    public void StopSpawning()
    {
        if (_isSpawning)
            _isSpawning = false;
    }

    private Node SpawnEnemy(BiomeDataReader.EnemyDefinition def)
    {
        if (def == null)
            return null;

        string scenePath = def.Scene;

        if (string.IsNullOrEmpty(scenePath))
        {
            GD.PrintErr($"LevelSpawner: no scene path for enemy '{def.Id}'");
            return null;
        }

        var packed = GD.Load<PackedScene>(scenePath);
        if (packed == null)
        {
            GD.PrintErr($"LevelSpawner: failed to load PackedScene '{scenePath}' for enemy '{def.Id}'");
            return null;
        }

        Node2D inst = packed.Instantiate() as Node2D;
        if (inst == null)
        {
            GD.PrintErr($"LevelSpawner: instantiate returned null for '{scenePath}'");
            return null;
        }

        inst.Position = GetSpawnPositionOutsideViewport();
        _spawnParent.AddChild(inst);

        return inst;
    }

    public void SpawnStartWave()
    {
        SpawnWave(StartWave);
    }
}
