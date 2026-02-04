using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class BaseFragment : EnemyBase
{
    public override string GenerateChallengeText()
    {
        return PickWord();
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override double GenerateTimeLimit(string word)
    {
        return BaseTimeForWord(word);
    }
}
