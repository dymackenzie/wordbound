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

        ConfigureTypingChallenge(Guid.NewGuid().ToString(), _typingChallenge);
    }

    private string GenerateShortWord(int maxLen)
    {
        return "test"; // placeholder implementation
    }

    protected override void ConfigureTypingChallenge(string id, TypingChallenge challenge)
    {
        if (challenge == null)
            return;

        string shortWord = GenerateShortWord(MaxShortWordLength);
        double timeLimit = Math.Max(0.8, shortWord.Length * 0.6);
        challenge.Prepare(id, shortWord, timeLimit);
    }
}
