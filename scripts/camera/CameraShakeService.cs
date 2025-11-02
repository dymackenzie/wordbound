using Godot;
using System;

public sealed class CameraShakeService : ICameraShakeService
{
    private double _timeLeft = 0.0;
    private float _magnitude = 0f;
    private readonly RandomNumberGenerator _rng = new();

    public CameraShakeService()
    {
        _rng.Randomize();
        CurrentOffset = Vector2.Zero;
    }

    public Vector2 CurrentOffset { get; private set; }

    public void Update(double delta)
    {
        if (_timeLeft <= 0.0)
        {
            CurrentOffset = Vector2.Zero;
            return;
        }

        _timeLeft -= delta;
        var x = _rng.RandfRange(-1f, 1f) * _magnitude;
        var y = _rng.RandfRange(-1f, 1f) * _magnitude;
        CurrentOffset = new Vector2(x, y);
    }

    public void Shake(double duration, float magnitude)
    {
        _timeLeft = Math.Max(_timeLeft, duration);
        _magnitude = Math.Max(_magnitude, magnitude);
    }

    public void Reset()
    {
        _timeLeft = 0.0;
        _magnitude = 0f;
        CurrentOffset = Vector2.Zero;
    }
}
