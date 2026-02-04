using Godot;
using System;
using System.Collections.Generic;

public static class WordPoolService
{
    private const double DefaultSecondsPerChar = 0.45; // seconds per character at difficulty 1

    public static string GetWord(int baseComplexity, int difficulty)
    {
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            var gs = tree.Root.GetNodeOrNull<GameState>("GameState");
            int userDifficulty = Math.Max(1, difficulty);
            int targetBucket = Math.Max(1, baseComplexity + (userDifficulty - 1));
            string poolPath = gs.GetPoolPathForDifficulty(userDifficulty);

            var pool = WordPoolLoader.LoadPool(poolPath);
            if (pool == null || pool.Count == 0) 
                return "word";


            // find best matching bucket (fall back to nearest lower)
            int chosenBucket = -1;
            for (int b = targetBucket; b >= 1; b--)
            {
                if (pool.ContainsKey(b))
                {
                    chosenBucket = b;
                    break;
                }
            }
            if (chosenBucket == -1)
            {
                foreach (var k in pool.Keys)
                    chosenBucket = Math.Max(chosenBucket, k);
                if (chosenBucket == -1) 
                    return "word";
            }

            var list = pool[chosenBucket];
            if (list == null || list.Count == 0) 
                return "word";

            var rng = new RandomNumberGenerator();
            rng.Randomize();
            int idx = rng.RandiRange(0, Math.Max(0, list.Count - 1));
            return list[idx];
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WordPoolService.GetWord failed: {ex.Message}");
            return "word";
        }
    }

    /// <summary>
    /// Preload word pools from the given paths.
    /// </summary>
    public static void PreloadPools(IEnumerable<string> paths)
    {
        if (paths == null) 
            return;
        foreach (var p in paths)
            WordPoolLoader.LoadPool(p);
    }

    /// <summary>
    ///  Get the seconds per character for the given difficulty.
    /// </summary>
    public static double GetSecondsPerChar(int difficulty)
    {
        int d = Math.Max(1, difficulty);
        return DefaultSecondsPerChar / d;
    }

    /// <summary>
    /// Compute a sensible base time for a given word and difficulty.
    /// This matches previous logic: baseTime = max(1.0, len * perChar) and a minimum floor of 0.75.
    /// </summary>
    public static double GetBaseTimeForWord(string word, int difficulty)
    {
        if (string.IsNullOrEmpty(word)) return 3.0;
        double perChar = GetSecondsPerChar(difficulty);
        double baseTime = Math.Max(1.0, word.Length * perChar);
        return Math.Max(0.75, baseTime);
    }
}
