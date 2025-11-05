using Godot;
using System;
using System.Collections.Generic;

public partial class AuraTimer : Control
{
    [Export] public NodePath AuraPath = new();
    [Export] public float Radius = 48f;
    [Export] public float Thickness = 6f;
    [Export] public Color ForegroundColor = new(0.9f, 0.7f, 0.2f, 1f);
    [Export] public Color BackgroundColor = new(0.2f, 0.2f, 0.2f, 0.6f);
    [Export] public int SegmentCount = 48; // resolution of the arc
    [Export] public float StartAngleDegrees = -90f; // top by default
    [Export] public bool Clockwise = true;

    private Node _auraNode;
    private Aura _aura;
    private bool _isTracking = false;

    public override void _Ready()
    {
        if (AuraPath.ToString() != string.Empty)
        {
            _auraNode = GetNodeOrNull(AuraPath);
            _aura = _auraNode as Aura;
        }

        if (_aura != null)
        {
            _aura.Connect("AuraActivatedEventHandler", new Callable(this, nameof(OnAuraActivated)));
            _aura.Connect("AuraEndedEventHandler", new Callable(this, nameof(OnAuraEnded)));
        }

        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_aura != null && _aura.IsActive)
        {
            _isTracking = true;
            QueueRedraw();
        }
        else if (_isTracking)
        {
            // one last redraw to clear the arc
            _isTracking = false;
            QueueRedraw();
        }
    }

    private void OnAuraActivated()
    {
        _isTracking = true;
        QueueRedraw();
    }

    private void OnAuraEnded(bool success)
    {
        _isTracking = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var center = GetRect().Size / 2.0f;
        if (_aura == null || !_aura.IsActive)
            return;

        double remaining = _aura.GetRemainingTimeSeconds();
        double max = _aura.GetMaxAuraTimeSeconds();
        if (max <= 0.0)
            return;

        double t = Math.Clamp(remaining / max, 0.0, 1.0);

        float startRad = Mathf.DegToRad(StartAngleDegrees);
        float remainingSweep = (float)(t * Math.PI * 2.0);
        if (!Clockwise)
            remainingSweep = -remainingSweep;
        float endRad = startRad + remainingSweep;

        // draw a filled, semi-transparent arc for the covered portion
        // so that the sprite behind it appears dimmed where time is spent.
        double coveredFraction = 1.0 - t;
        if (coveredFraction > 0.0001)
        {
            // covered arc goes from endRad -> startRad + sign*2PI
            float coveredStart = endRad;
            float twoPi = (float)(Math.PI * 2.0);
            float coveredEnd = Clockwise ? startRad + twoPi : startRad - twoPi;

            int coveredSegments = Math.Max(2, (int)(SegmentCount * coveredFraction));
            var polyPoints = new List<Vector2>
            {
                // center first (triangle-fan)
                center
            };
            for (int i = 0; i <= coveredSegments; i++)
            {
                float a = Mathf.Lerp(coveredStart, coveredEnd, (float)i / coveredSegments);
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
                polyPoints.Add(p);
            }

            // build colors array
            var colors = new Color[polyPoints.Count];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = BackgroundColor;

            DrawPolygon([.. polyPoints], colors);
        }

        // draw a clear edge for the remaining arc
        if (t > 0.0001)
        {
            int usedSegments = Math.Max(2, (int)(SegmentCount * t));
            var points = new List<Vector2>();
            for (int i = 0; i <= usedSegments; i++)
            {
                float a = Mathf.Lerp(startRad, endRad, (float)i / usedSegments);
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Radius;
                points.Add(p);
            }

            if (points.Count >= 2)
            {
                DrawPolyline([.. points], ForegroundColor, Thickness);
            }
        }
    }

    public void SetAura(Aura aura)
    {
        if (_aura != null)
        {
            _aura.Disconnect("AuraActivatedEventHandler", new Callable(this, nameof(OnAuraActivated)));
            _aura.Disconnect("AuraEndedEventHandler", new Callable(this, nameof(OnAuraEnded)));
        }

        _aura = aura;

        if (_aura != null)
        {
            _aura.Connect("AuraActivatedEventHandler", new Callable(this, nameof(OnAuraActivated)));
            _aura.Connect("AuraEndedEventHandler", new Callable(this, nameof(OnAuraEnded)));
        }
    }
}
