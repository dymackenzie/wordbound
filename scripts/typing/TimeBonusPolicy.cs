using System;

public interface ITimeBonusPolicy
{
    /// <summary>
    /// Apply a time bonus and return the new deadline ticks (ms).
    /// Parameters:
    ///  - currentDeadlineTicks: existing absolute deadline (ms)
    ///  - nowTicksMs: current tick time (ms)
    ///  - letterTimeBonusSeconds: how many seconds to add per correct letter
    ///  - maxRemainingSeconds: cap on remaining seconds
    /// </summary>
    ulong ApplyBonus(ulong currentDeadlineTicks, ulong nowTicksMs, double letterTimeBonusSeconds, double maxRemainingSeconds);
}

public class DefaultTimeBonusPolicy : ITimeBonusPolicy
{
    public ulong ApplyBonus(ulong currentDeadlineTicks, ulong nowTicksMs, double letterTimeBonusSeconds, double maxRemainingSeconds)
    {
        try
        {
            ulong bonusMs = (ulong)(letterTimeBonusSeconds * 1000.0);
            ulong maxMs = (ulong)(maxRemainingSeconds * 1000.0);
            ulong currentRemainingMs = (currentDeadlineTicks > nowTicksMs) ? (currentDeadlineTicks - nowTicksMs) : 0UL;
            ulong newRemainingMs = currentRemainingMs + bonusMs;
            if (newRemainingMs > maxMs) 
                newRemainingMs = maxMs;
            return nowTicksMs + newRemainingMs;
        }
        catch
        {
            return currentDeadlineTicks;
        }
    }
}
