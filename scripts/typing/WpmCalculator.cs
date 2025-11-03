using System;

public interface IWpmCalculator
{
    /// <summary>
    /// Calculate words per minute (WPM).
    /// Parameters:
    ///  - correctChars: number of correctly typed characters
    ///  - startTicksMs: start time in ticks (ms)
    ///  - nowTicksMs: current time in ticks (ms)
    /// </summary>
    double CalculateWpm(int correctChars, ulong startTicksMs, ulong nowTicksMs);
}

public class WpmCalculator : IWpmCalculator
{
    public double CalculateWpm(int correctChars, ulong startTicksMs, ulong nowTicksMs)
    {
        double elapsedMinutes = Math.Max(1e-6, (nowTicksMs - startTicksMs) / 60000.0);
        return correctChars / 5.0 / elapsedMinutes;
    }
}
