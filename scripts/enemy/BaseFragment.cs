using Godot;
using System;

public partial class BaseFragment : EnemyBase
{
    public override string GenerateChallengeText()
    {
        return "";
    }

    public override double GenerateTimeLimit(string word)
    {
        return 0;
    }
}
