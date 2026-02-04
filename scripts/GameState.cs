using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameState : Node
{
    [Signal] public delegate void SeedsChangedEventHandler(int newTotal);

    private const int CURRENT_SAVE_VERSION = 1;
    private const string SAVE_PATH = "user://save.json";

    private IFileRepository _fileRepo;
    private SaveManager _saveManager;

    public int Seeds { get; private set; } = 0;
    public int Difficulty { get; set; } = 1;

    public List<string> UnlockedRelics { get; private set; } = [];
    public Dictionary<string, object> Conservatory { get; private set; } = [];

    public override void _Ready()
    {
        _fileRepo = new FileRepository();
        _saveManager = new SaveManager(SAVE_PATH, _fileRepo, CURRENT_SAVE_VERSION);

        Load();
    }

    /// <summary>
    /// Adds the given amount of seeds to the player's total.
    /// </summary>
    public void AddSeeds(int amount)
    {
        if (amount <= 0)
            return;
        Seeds += amount;
        EmitSignal(nameof(SeedsChanged), Seeds);
    }

    /// <summary>
    /// Attempts to spend the given amount of seeds.
    /// </summary>
    public bool SpendSeeds(int amount)
    {
        if (amount <= 0)
            return false;
        if (Seeds < amount)
            return false;
        Seeds -= amount;
        EmitSignal(nameof(SeedsChanged), Seeds);
        return true;
    }

    /// <summary>
    /// Saves the current game state.
    /// </summary>
    public void Save()
    {
        var payload = new SavePayload
        {
            Seeds = Seeds,
            Difficulty = Difficulty,
            UnlockedRelics = UnlockedRelics,
            Conservatory = Conservatory
        };

        _saveManager.Save(payload);
    }

    /// <summary>
    /// Loads the game state from the save file.
    /// </summary>
    public void Load()
    {
        var payload = _saveManager.LoadOrDefault();

        Seeds = payload?.Seeds ?? 0;

        Difficulty = payload?.Difficulty ?? 1;

        UnlockedRelics.Clear();
        if (payload?.UnlockedRelics != null)
        {
            foreach (var unlockedRelic in payload.UnlockedRelics)
                UnlockedRelics.Add(unlockedRelic);
        }
        Conservatory = payload?.Conservatory ?? [];

        EmitSignal(nameof(SeedsChanged), Seeds);
    }

    /// <summary>
    /// Mapping from difficulty levels to word pool file paths.
    /// </summary>
    public Dictionary<int, string> DifficultyPoolMap { get; set; } = new Dictionary<int, string>
    {
        { 1, "res://data/words/pools/english_1k_pool.json" },
        { 2, "res://data/words/pools/english_5k_pool.json" },
        { 3, "res://data/words/pools/english_10k_pool.json" },
        { 4, "res://data/words/pools/english_25k_pool.json" }
    };

    /// <summary>
    /// Gets the appropriate word pool file path for the given difficulty level.
    /// </summary>
    public string GetPoolPathForDifficulty(int difficulty)
    {
        if (DifficultyPoolMap == null || DifficultyPoolMap.Count == 0)
            return "res://data/words/pools/english_5k_pool.json";

        // prefer the highest configured key <= difficulty, otherwise the highest available
        var keys = DifficultyPoolMap.Keys.OrderByDescending(k => k).ToList();
        foreach (var k in keys)
        {
            if (k <= difficulty)
                return DifficultyPoolMap[k];
        }
        return DifficultyPoolMap[keys.First()];
    }
}
