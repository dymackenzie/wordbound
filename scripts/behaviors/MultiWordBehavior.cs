using Godot;
using System;
using System.Collections.Generic;

public partial class MultiWordBehavior : ChallengeBehavior
{
    [Export]
    public int WordCount { get; set; } = 2;

    public override string TransformWord(string word)
    {
        // `word` is the first chosen word; fetch additional words via service
        var parts = new List<string> { word };
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        GameState gs = tree.Root.GetNodeOrNull<GameState>("GameState");
        int userDifficulty = Math.Max(1, gs.Difficulty);
        int baseComplexity = Math.Max(1, word.Length);
        for (int i = 1; i < WordCount; i++)
        {
            parts.Add(WordPoolService.GetWord(baseComplexity, userDifficulty));
        }
        return string.Join(" ", parts);
    }

    public override double GetTimeLimit(string baseWord, double baseTime)
    {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        GameState gs = tree.Root.GetNodeOrNull<GameState>("GameState");
        int userDifficulty = Math.Max(1, gs.Difficulty);

        if (string.IsNullOrEmpty(baseWord)) 
            return Math.Max(1.0, baseTime);
        
        var parts = baseWord.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) 
            return Math.Max(1.0, baseTime);

        double total = 0.0;
        foreach (var p in parts)
            total += WordPoolService.GetBaseTimeForWord(p, userDifficulty);

        return Math.Max(1.0, total);
    }
}
