using Godot;
using System;

public partial class SpeedyFragment : EnemyBase
{
    [Export] public float SpeedMultiplier { get; set; } = 1.8f;
    [Export] public float ShortSurroundDuration { get; set; } = 0.5f;
    [Export] public float ShortOrbitDuration { get; set; } = 1.0f;
    [Export] public int MaxShortWordLength { get; set; } = 4;

    public override void _Ready()
    {
        base._Ready();

        // buff movement and shorten surround/orbit times for a speedy fragment
        MoveSpeed *= SpeedMultiplier;
        SurroundDuration = Math.Min(SurroundDuration, ShortSurroundDuration);
        OrbitDuration = Math.Min(OrbitDuration, ShortOrbitDuration);
    }

    public override string GenerateChallengeText()
    {
        return "";
    }

    public override double GenerateTimeLimit(string word)
    {
        return 0;
    }
}
