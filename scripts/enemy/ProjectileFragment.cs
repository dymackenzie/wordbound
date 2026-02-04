using Godot;
using System;

public partial class ProjectileFragment : ProjectileBase
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
        double scrambledTime = Behavior.GetTimeLimit(word, baseTime);
        return Math.Max(0.5, scrambledTime);
    }

    public override void _Ready()
    {
        base._Ready();
        Behavior = new ScrambleBehavior();
    }
}
