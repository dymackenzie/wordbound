using Godot;
using System;

public partial class MirrorFragment : EnemyBase
{
    public override string GenerateChallengeText()
    {
        return Utility.ReverseString("");
    }

    public override double GenerateTimeLimit(string word)
    {
        return 0;
    }
}
