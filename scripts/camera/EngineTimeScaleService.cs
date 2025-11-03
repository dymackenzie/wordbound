using Godot;

public interface ITimeScaleService
{
    /// <summary>
    /// Get the current engine time scale.
    /// </summary>
    double Current { get; }

    /// <summary>
    /// Set the engine time scale.
    /// </summary>
    void SetScale(double scale);

    /// <summary>
    /// Restore the engine time scale to its original value.
    /// </summary>
    void Restore();
}

public sealed class EngineTimeScaleService : ITimeScaleService
{
    private double _original = 1.0;
    private bool _hasOriginal = false;

    public double Current => Engine.TimeScale;

    public void SetScale(double scale)
    {
        if (!_hasOriginal)
        {
            _original = Engine.TimeScale;
            _hasOriginal = true;
        }
        Engine.TimeScale = scale;
    }

    public void Restore()
    {
        if (_hasOriginal)
        {
            Engine.TimeScale = _original;
            _hasOriginal = false;
        }
    }
}
