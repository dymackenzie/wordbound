using Godot;
using System;
using System.Text;

public partial class ScrambleBehavior : ChallengeBehavior
{
    public override string TransformWord(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length <= 2) return word;
        var rand = new RandomNumberGenerator();
        rand.Randomize();
        var chars = word.ToCharArray();

        // Fisher-Yates shuffle on internal letters to keep first/last for readability
        for (int i = chars.Length - 2; i > 1; i--)
        {
            int j = rand.RandiRange(1, i);
            (chars[j], chars[i]) = (chars[i], chars[j]);
        }
        return new string(chars);
    }

    public override double GetTimeLimit(string baseWord, double baseTime)
    {
        // give a bit more time for scrambled words
        return Math.Max(0.75, baseTime * 1.25);
    }
}
