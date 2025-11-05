using System;

public static class AccuracyCalculator
{
    /// <summary>
    /// Compute accuracy between expected and typed strings.
    /// </summary>
    public static double ComputeAccuracy(string expected, string typed)
    {
        if (expected == null) expected = "";
        if (typed == null) typed = "";

        int total = expected.Length;
        if (total == 0)
            return typed.Length == 0 ? 1.0 : 0.0;

        int min = Math.Min(expected.Length, typed.Length);
        int correct = 0;
        for (int i = 0; i < min; i++)
        {
            if (expected[i] == typed[i])
                correct++;
        }

        // clamp
        double acc = (double)correct / (double)total;
        if (acc < 0.0) 
            acc = 0.0;
        if (acc > 1.0) 
            acc = 1.0;
        return acc;
    }
}
