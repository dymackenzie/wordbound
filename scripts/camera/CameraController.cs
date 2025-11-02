using Godot;
using System;

public partial class CameraController : Camera2D
{
    [Signal] public delegate void CinematicStartedEventHandler();
    [Signal] public delegate void CinematicEndedEventHandler();

    [Export] public Vector2 KillZoom { get; set; } = new Vector2(0.6f, 0.6f);
    [Export] public float ZoomDuration { get; set; } = 0.18f;
    [Export] public double KillTimeScale { get; set; } = 0.22;

    private Vector2 _originalZoom;
    private ITimeScaleService _timeService;
    private Node2D _target;
    private bool _isInCinematic = false;

    public override void _Ready()
    {
        _timeService ??= new EngineTimeScaleService();
        _originalZoom = Zoom;
    }

    public void SetTarget(Node2D target)
    {
        _target = target;
        MakeCurrent();
    }

    public override void _Process(double delta)
    {
        if (_target != null)
            GlobalPosition = _target.GlobalPosition;
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
        tween.TweenCallback(Callable.From(() => {
            _timeService.Restore();
            EmitSignal(nameof(CinematicEnded));
        }));
    }
}
