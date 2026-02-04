using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class BaseFragment : EnemyBase
{
    [Export]
    public ChallengeBehavior Behavior { get; set; }

    public override string GenerateChallengeText()
    {
        int enemyComplexity = Math.Max(1, EnemyComplexity);
        int biomeComplexity = Math.Max(0, BiomeBaseComplexity);
        int baseComplexity = Math.Max(1, enemyComplexity + biomeComplexity);

        GameState gs = GetTree().Root.GetNodeOrNull<GameState>("GameState");
        int userDifficulty = Math.Max(1, gs.Difficulty);

        string baseWord = WordPoolService.GetWord(baseComplexity, userDifficulty);
        string result = baseWord;
        if (Behavior != null)
        {
            result = Behavior.TransformWord(baseWord);
            Behavior.OnAssigned(this, baseWord);
        }

        return result;
    }

    public override double GenerateTimeLimit(string word)
    {
        if (string.IsNullOrEmpty(word))
            return 3.0; // default time limit

        /**
        * Simple time limit calculation:
        * Base time is 0.45 seconds per character at difficulty 1.
        * Time scales down with user difficulty.
        */
        GameState gs = GetTree().Root.GetNodeOrNull<GameState>("GameState");
        int userDifficulty = Math.Max(1, gs.Difficulty);

        double baseTime = WordPoolService.GetBaseTimeForWord(word, userDifficulty);
        if (Behavior != null)
            return Behavior.GetTimeLimit(word, baseTime);
        return baseTime;
    }
}
