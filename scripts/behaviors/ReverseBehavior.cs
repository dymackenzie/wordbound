using Godot;
using System;

public partial class ReverseBehavior : ChallengeBehavior
{
    public override string TransformWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;
        var arr = word.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}
