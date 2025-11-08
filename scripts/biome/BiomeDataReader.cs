using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class BiomeDataReader
{
    public class EnemyDefinition
    {
        /// <summary>
        /// The unique id of the enemy.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// The complexity rating of the enemy which influences sentence generation difficulty.
        /// </summary>
        public int Complexity { get; set; } = 1;

        /// <summary>
        /// The spawn weight of the enemy which influences budget-based selection.
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// The cost of the enemy which is deducted from the budget when spawning.
        /// </summary>
        public int Cost { get; set; } = 1;

        /// <summary>
        /// The tags associated with the enemy to determine behavior.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// The components associated with the enemy to determine sentence structure.
        /// </summary>
        public List<string> Components { get; set; } = [];

        /// <summary>
        /// The number of seeds rewarded for defeating the enemy.
        /// </summary>
        public int RewardSeeds { get; set; } = 0;

        /// <summary>
        /// The scene path to the enemy's in-game representation.
        /// </summary>
        public string Scene { get; set; } = "";
    }

    public class BiomeDefinition
    {
        /// <summary>
        /// The unique id of the biome.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// The base complexity which is then added on to by the enemies complexities.
        /// </summary>
        public int BaseComplexity { get; set; } = 0;

        /// <summary>
        /// The base budget for spawning enemies in the biome. This is subtracted by each enemies weight until depleted.
        /// </summary>
        public int BaseBudget { get; set; } = 0;

        /// <summary>
        /// The amount to increase the budget by each wave.
        /// </summary>
        public int BudgetAdditionPerWave { get; set; } = 0;

        /// <summary>
        /// The number of waves in the biome.
        /// </summary>
        public int Waves { get; set; } = 1;

        /// <summary>
        /// The spawn mode for the biome (stagger, burst).
        /// </summary>
        public string SpawnMode { get; set; } = "stagger";

        /// <summary>
        /// The pool of enemies that can spawn in the biome. This may contain enemy ids or inline enemy definitions.
        /// </summary>
        public List<EnemyDefinition> Pool { get; set; } = [];
    }

    /// <summary>
    /// Load a BiomeDefinition from a biome JSON file. The biome file's pool may contain
    /// enemy ids (strings) or inline enemy definition objects.
    /// </summary>
    public static BiomeDefinition LoadBiomeFromFile(string path)
    {
        var biome = new BiomeDefinition();
        if (string.IsNullOrEmpty(path))
            return biome;

        try
        {
            using (var f = FileAccess.Open(path, FileAccess.ModeFlags.Read))
            {
                var text = f.GetAsText();
                using (var doc = JsonDocument.Parse(text))
                {
                    var root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        GD.PrintErr($"BiomeDataReader: expected object root in biome file '{path}'");
                        return biome;
                    }

                    if (root.TryGetProperty("id", out var idElem) && idElem.ValueKind == JsonValueKind.String)
                        biome.Id = idElem.GetString();

                    if (root.TryGetProperty("base_complexity", out var bc) && bc.ValueKind == JsonValueKind.Number)
                        biome.BaseComplexity = bc.GetInt32();
                    if (root.TryGetProperty("base_budget", out var bb) && bb.ValueKind == JsonValueKind.Number)
                        biome.BaseBudget = bb.GetInt32();
                    if (root.TryGetProperty("budget_addition_per_wave", out var bapw) && bapw.ValueKind == JsonValueKind.Number)
                        biome.BudgetAdditionPerWave = bapw.GetInt32();
                    if (root.TryGetProperty("waves", out var wv) && wv.ValueKind == JsonValueKind.Number)
                        biome.Waves = wv.GetInt32();
                    if (root.TryGetProperty("spawn_mode", out var sm) && sm.ValueKind == JsonValueKind.String)
                        biome.SpawnMode = sm.GetString();

                    // pool parsing
                    if (root.TryGetProperty("pool", out var poolElem) && poolElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in poolElem.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var id = item.GetString();
                                var enemy = LoadEnemyDefinitionById(id);
                                if (enemy != null)
                                    biome.Pool.Add(enemy);
                            }
                            else if (item.ValueKind == JsonValueKind.Object)
                            {
                                var enemy = ParseEnemyDefinition(item);
                                if (enemy != null)
                                    biome.Pool.Add(enemy);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"BiomeDataReader: error loading biome file '{path}': {ex.Message}");
        }

        return biome;
    }

    /// <summary>
    /// Note: loads an enemy definition by id, expecting the file to be at
    /// `res://data/enemies/{id}.json` unless a path is given.
    /// </summary>
    public static EnemyDefinition LoadEnemyDefinitionById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        // normalize id into a file path
        var candidate = id;
        if (!candidate.StartsWith("res://") && !candidate.Contains('/'))
        {
            candidate = $"res://data/enemies/{id}.json";
        }

        return LoadEnemyDefinitionFromFile(candidate);
    }

    /// <summary>
    /// Load an enemy definition from a given file path.
    /// </summary>
    public static EnemyDefinition LoadEnemyDefinitionFromFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            using (var f = FileAccess.Open(path, FileAccess.ModeFlags.Read))
            {
                var text = f.GetAsText();
                using (var doc = JsonDocument.Parse(text))
                {
                    var root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                        return null;
                    return ParseEnemyDefinition(root);
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"BiomeDataReader: error loading enemy file '{path}': {ex.Message}");
            return null;
        }
    }

    private static EnemyDefinition ParseEnemyDefinition(JsonElement elem)
    {
        if (elem.ValueKind != JsonValueKind.Object)
            return null;

        var def = new EnemyDefinition();

        if (elem.TryGetProperty("id", out var idElem) && idElem.ValueKind == JsonValueKind.String)
            def.Id = idElem.GetString();

        if (elem.TryGetProperty("complexity", out var c) && c.ValueKind == JsonValueKind.Number)
            def.Complexity = c.GetInt32();
        if (elem.TryGetProperty("cost", out var costElem) && costElem.ValueKind == JsonValueKind.Number)
            def.Cost = costElem.GetInt32();

        if (elem.TryGetProperty("reward_seeds", out var rs) && rs.ValueKind == JsonValueKind.Number)
            def.RewardSeeds = rs.GetInt32();
        if (elem.TryGetProperty("scene", out var sc) && sc.ValueKind == JsonValueKind.String)
            def.Scene = sc.GetString();

        if (elem.TryGetProperty("tags", out var tagsElem) && tagsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tagsElem.EnumerateArray())
                if (t.ValueKind == JsonValueKind.String)
                    def.Tags.Add(t.GetString());
        }

        if (elem.TryGetProperty("components", out var compElem) && compElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var cElem in compElem.EnumerateArray())
                if (cElem.ValueKind == JsonValueKind.String)
                    def.Components.Add(cElem.GetString());
        }

        return def;
    }
}
