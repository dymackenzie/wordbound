using Godot;
using System;

public partial class MirrorWordBehavior : EnemyBase
{
    public override void _Ready()
    {
        base._Ready();

        ConfigureTypingChallenge(Guid.NewGuid().ToString(), _typingChallenge);
    }
    
    protected override void ConfigureTypingChallenge(string id, TypingChallenge challenge)
    {
        if (challenge == null)
            return;

        string mirrored = Utility.ReverseString("example"); // placeholder word
        double timeLimit = Math.Max(0.8, mirrored.Length * 0.6);
        challenge.Prepare(id, mirrored, timeLimit);
    }
}
