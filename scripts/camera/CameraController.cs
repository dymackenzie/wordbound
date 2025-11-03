using Godot;
using System;

public partial class CameraController : Camera2D
{
    [Signal] public delegate void CinematicStartedEventHandler();
    [Signal] public delegate void CinematicEndedEventHandler();

    [Export] public Vector2 KillZoom { get; set; } = new Vector2(0.6f, 0.6f);
    [Export] public float ZoomDuration { get; set; } = 0.18f;
    [Export] public double KillTimeScale { get; set; } = 0.22;
    [Export] public double ShakeDuration { get; set; } = 0.12;
    [Export] public float ShakeMagnitude { get; set; } = 6f;

    private Vector2 _originalZoom;
    private ITimeScaleService _timeService;
    private Node2D _target;
    private bool _isInCinematic = false;

    private ICameraShakeService _shakeService;

    public override void _Ready()
    {
        _timeService ??= new EngineTimeScaleService();
        _originalZoom = Zoom;
        _shakeService ??= new CameraShakeService();
    }

    public void SetTarget(Node2D target)
    {
        _target = target;
        MakeCurrent();
    }

    public override void _Process(double delta)
    {
        if (_target != null)
            GlobalPosition = _target.GlobalPosition + _shakeService.CurrentOffset;

        _shakeService.Update(delta);
    }

    public void StartKillCinematic()
    {
        if (_isInCinematic) return;
        _isInCinematic = true;
        _originalZoom = Zoom;
        _timeService.SetScale(KillTimeScale);
        EmitSignal(nameof(CinematicStarted));
        var tween = CreateTween();
        tween.TweenProperty(this, "zoom", KillZoom, ZoomDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => {
            // just in case we need this
        }));
    }

    public void EndKillCinematic()
    {
        if (!_isInCinematic) return;
        _isInCinematic = false;
        var tween = CreateTween();
        tween.TweenProperty(this, "zoom", _originalZoom, ZoomDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() =>
        {
            _timeService.Restore();
            EmitSignal(nameof(CinematicEnded));
        }));
    }

    public void Shake(double duration, float magnitude)
    {
        _shakeService?.Shake(duration, magnitude);
    }

    public void OnCharacterTyped(string ch)
    {
        _shakeService?.Shake(ShakeDuration, ShakeMagnitude);
    }
}
