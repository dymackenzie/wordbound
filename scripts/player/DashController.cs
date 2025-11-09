using Godot;
using System;

public class DashController
{
    public bool IsDashing { get; private set; } = false;

    public float DashDuration { get; set; } = 0.16f;
    public float DashDistance { get; set; } = 180f;
    public float DashStopDistance { get; set; } = 24f;

    public PackedScene GhostScene { get; set; } = null;
    public bool GhostEnabled { get; set; } = true;
    public float GhostLifetimeOverride { get; set; } = 0f;
    public float GhostInitialOpacity { get; set; } = 0.6f;

    public event Action OnDashed;
    public event Action OnDashArrived;
    public event Action OnDashEnded;

    private Vector2 _dashVelocity = Vector2.Zero;
    private float _timeLeft = 0f;
    private Vector2? _targetPosition = null;

    public void StartDash(CharacterBody2D owner, Vector2 direction, float dashDistance, float duration)
    {
        if (direction.LengthSquared() <= 0.0f)
            return;

        direction = direction.Normalized();
        DashDistance = dashDistance;
        DashDuration = duration;

        _dashVelocity = direction * (DashDistance / DashDuration);
        _timeLeft = DashDuration;
        _targetPosition = null;
        IsDashing = true;
        OnDashed?.Invoke();

        TrySpawnGhost(owner);
    }

    public void StartDashTowards(CharacterBody2D owner, Vector2 worldPosition, float dashDistance, float duration, float stopDistance)
    {
        DashDistance = dashDistance;
        DashDuration = duration;
        DashStopDistance = stopDistance;

        _targetPosition = worldPosition;
        _dashVelocity = Vector2.Zero;
        _timeLeft = DashDuration;
        IsDashing = true;
        OnDashed?.Invoke();

        TrySpawnGhost(owner);
    }

    /// <summary>
    /// Update dash physics. Returns true if the owner is currently dashing and
    /// the physics step was applied (caller may early-return).
    /// </summary>
    public bool Update(CharacterBody2D owner, double delta)
    {
        if (!IsDashing)
            return false;

        // ensure we have a velocity if we were given a target position
        if (_targetPosition.HasValue && _dashVelocity.LengthSquared() <= 0.0001f)
        {
            var dir = _targetPosition.Value - owner.GlobalPosition;
            if (dir.LengthSquared() > 0.0001f)
                _dashVelocity = dir.Normalized() * (DashDistance / DashDuration);
        }

        _timeLeft -= (float)delta;
        owner.Velocity = _dashVelocity;
        owner.MoveAndSlide();

        // arrival check
        if (_targetPosition.HasValue)
        {
            var pos = owner.GlobalPosition;
            var targ = _targetPosition.Value;
            if (pos.DistanceTo(targ) <= DashStopDistance)
            {
                IsDashing = false;
                _targetPosition = null;
                owner.Velocity = Vector2.Zero;
                OnDashArrived?.Invoke();
                OnDashEnded?.Invoke();
                return true;
            }
        }

        // time up
        if (_timeLeft <= 0f && IsDashing)
        {
            IsDashing = false;
            OnDashEnded?.Invoke();
        }

        return true;
    }

    public void EndDash()
    {
        if (!IsDashing)
            return;
        IsDashing = false;
        _targetPosition = null;
        _timeLeft = 0f;
        OnDashEnded?.Invoke();
    }

    private void TrySpawnGhost(CharacterBody2D owner)
    {
        if (owner == null || !GhostEnabled || GhostScene == null)
            return;

        if (GhostScene.Instantiate() is not DashGhost instance)
            return;

        owner.GetParent().AddChild(instance);

        instance.GlobalPosition = owner.GlobalPosition;
        instance.GlobalRotation = owner.GlobalRotation;
        instance.Scale = owner.Scale;

        if (GhostLifetimeOverride > 0f)
            instance.Lifetime = GhostLifetimeOverride;
        instance.InitialOpacity = GhostInitialOpacity;
    }
}
