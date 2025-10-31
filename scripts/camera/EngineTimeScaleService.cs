using Godot;

public sealed class EngineTimeScaleService : ITimeScaleService
{
    private float _original = 1f;
    private bool _hasOriginal = false;

    public float Current => Engine.TimeScale;

    public void SetScale(float scale)
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
