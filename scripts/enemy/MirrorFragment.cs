using Godot;
using System;

public partial class MirrorFragment : EnemyBase
{
    public override string GenerateChallengeText()
    {
        string baseWord = PickWord();
        var word = Behavior.TransformWord(baseWord);
        Behavior.OnAssigned(this, baseWord);
        return word;
    }

    public override double GenerateTimeLimit(string word)
    {
        double baseTime = BaseTimeForWord(word);
        double mirroredTime = Behavior.GetTimeLimit(word, baseTime);
        return Math.Max(0.6, mirroredTime);
    }

    public override void _Ready()
    {
        base._Ready();
        Behavior = new ReverseBehavior();
    }
}
