using Godot;
using System;

public partial class HeavyFragment : EnemyBase
{
    [Export] public float SpeedMultiplier { get; set; } = 0.6f;
    [Export] public float LongSurroundDuration { get; set; } = 1.5f;
    [Export] public float LongOrbitDuration { get; set; } = 2.5f;
    [Export] public int SentenceWordCount { get; set; } = 2;
    [Export] public double BaseTimeSeconds { get; set; } = 1.5;
    [Export] public double TimePerCharSeconds { get; set; } = 0.28;

    public override void _Ready()
    {
        base._Ready();

        // make movement slower and increase surround/orbit durations for a heavy, looming fragment
        MoveSpeed *= SpeedMultiplier;
        SurroundDuration = Math.Max(SurroundDuration, LongSurroundDuration);
        OrbitDuration = Math.Max(OrbitDuration, LongOrbitDuration);

        Behavior = new MultiWordBehavior() { WordCount = SentenceWordCount };
    }

    public override string GenerateChallengeText()
    {
        string word = PickWord();
        var sentence = Behavior.TransformWord(word);
        Behavior.OnAssigned(this, word);
        return sentence;
    }

    public override double GenerateTimeLimit(string word)
    {
        double baseTime = BaseTimeForWord(word);
        double mirroredTime = Behavior.GetTimeLimit(word, baseTime);
        return Math.Max(0.6, mirroredTime);
    }
}