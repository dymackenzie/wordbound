using Godot;
using System;

public partial class ProjectileFragment : ProjectileBase
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
