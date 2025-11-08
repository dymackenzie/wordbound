using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [Signal] public delegate void AuraActivatedEventHandler();
    [Signal] public delegate void DashedEventHandler();
    [Signal] public delegate void DashArrivedEventHandler();
    [Signal] public delegate void RelicEquippedEventHandler(string relicId);
    [Signal] public delegate void RelicUnequippedEventHandler(string relicId);
    [Signal] public delegate void AnimationStartedEventHandler(string animationName);
    [Signal] public delegate void AnimationFinishedEventHandler(string animationName);

    [Export] public float Speed { get; set; } = 220f;
    [Export] public float DashDistance { get; set; } = 180f;
    [Export] public float DashCooldown { get; set; } = 1.0f;
    [Export] public float DashInvulnerabilityTime { get; set; } = 0.12f;
    [Export] public bool CanDash { get; set; } = false;
    [Export] public float DashStopDistance { get; set; } = 24f;

    [Export] public NodePath AnimationPlayerPath { get; set; } = new NodePath("AnimationPlayer");
    [Export] public string IdleAnimationName { get; set; } = "idle";
    [Export] public string RunAnimationName { get; set; } = "run";
    [Export] public string DashAnimationName { get; set; } = "dash";
    [Export] public string AttackAnimationName { get; set; } = "attack";
    [Export] public double AnimationBlendSeconds { get; set; } = 0.12;
    [Export] public float RunAnimationThreshold { get; set; } = 6.0f; // velocity magnitude to consider running
    [Export] public float AttackHoldPosition { get; set; } = 0.5f; // normalized 0..1 of animation length to hold at

    private float _lastDashAt = -999f; // seconds (OS ticks)
    private bool _isDashing = false;
    private Vector2 _dashVelocity = Vector2.Zero;
    private float _dashTimeLeft = 0f;
    private const float _dashDuration = 0.16f;

    private Vector2? _dashTargetPosition = null;

    private readonly HashSet<string> _equippedRelics = [];

    private AnimationPlayer _animationPlayer = null;
    private AnimationStateController _animController = null;
    private string _currentAnimation = "";
    private bool _isAttacking = false;
    private bool _isAttackHeld = false;

    public AnimationState CurrentAnimationState { get; set; } = AnimationState.Idle;

    public override void _Ready()
    {
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _animationPlayer.Connect("animation_finished", new Callable(this, nameof(OnAnimationPlayerFinished)));

        InitAnimationController();
    }

    private void InitAnimationController()
    {
        _animController = new AnimationStateController(this)
        {
            BlendSeconds = AnimationBlendSeconds,
            RunThreshold = RunAnimationThreshold
        };
    }

    private void UpdateAnimationState()
    {
        _animController?.Update(Velocity, _isDashing, _isAttacking);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (HandleDashPhysics(delta))
            return;

        var input = new Vector2(
            Input.GetActionStrength("d_right") - Input.GetActionStrength("a_left"),
            Input.GetActionStrength("s_down") - Input.GetActionStrength("w_up")
        );

        if (input.LengthSquared() > 0f)
            input = input.Normalized();

        Velocity = input * Speed;
        MoveAndSlide();
    }

    private bool HandleDashPhysics(double delta)
    {
        if (_isDashing)
        {
            _dashTimeLeft -= (float)delta;
            Velocity = _dashVelocity;
            MoveAndSlide();

            // if we have a target position, check if we've arrived
            if (_dashTargetPosition.HasValue)
            {
                var pos = GlobalPosition;
                var targ = _dashTargetPosition.Value;
                if (pos.DistanceTo(targ) <= DashStopDistance)
                {
                    _isDashing = false;
                    _dashTargetPosition = null;
                    Velocity = Vector2.Zero;
                    EmitSignal(nameof(DashArrived));
                    PrepareAttackHold();
                }
            }

            if (_dashTimeLeft <= 0f && _isDashing)
                _isDashing = false;
            return true;
        }
        return false;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("activate_aura"))
        {
            EmitSignal(nameof(AuraActivated));
        }
        if (Input.IsActionJustPressed("dash"))
        {
            Dash();
        }
    }

    private void OnAnimationPlayerFinished(string animName)
    {
        _currentAnimation = string.Empty;

        if (animName == AttackAnimationName)
            _isAttacking = false;
        EmitSignal(nameof(AnimationFinished), animName);
    }

    /// <summary>
    /// Plays the specified animation with optional blend time. Will start and finish signals so that
    /// callers can rely on them.
    /// </summary>
    public void PlayAnimation(string animName, double blend = 0.12)
    {
        EmitSignal(nameof(AnimationStarted), animName);

        if (!_animationPlayer.HasAnimation(animName))
        {
            EmitSignal(nameof(AnimationFinished), animName);
            return;
        }

        _currentAnimation = animName;
        _animationPlayer.Play(animName, customBlend: blend, customSpeed: 1.0f, fromEnd: false);
    }

    public void StopAnimation()
    {
        if (_animationPlayer != null)
            _animationPlayer.Stop();
        _currentAnimation = string.Empty;
    }

    public bool IsPlayingAnimation(string animName = null)
    {
        if (_animationPlayer == null)
            return false;
        if (string.IsNullOrEmpty(animName))
            return !string.IsNullOrEmpty(_currentAnimation);
        return _currentAnimation == animName;
    }

    public void Dash()
    {
        if (!CanDash)
            return;

        // use UTC time for cooldown timing so we don't depend on Engine/OS tick helpers
        var now = (float)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        if (now - _lastDashAt < DashCooldown)
            return;

        var input = new Vector2(
            Input.GetActionStrength("d_right") - Input.GetActionStrength("a_left"),
            Input.GetActionStrength("s_down") - Input.GetActionStrength("w_up")
        );

        if (input.LengthSquared() <= 0f)
            return; // no direction to dash

        input = input.Normalized();
        _isDashing = true;
        _dashTimeLeft = _dashDuration;
        // dash velocity set so the character travels roughly DashDistance over _dashDuration
        _dashVelocity = input * (DashDistance / _dashDuration);
        _lastDashAt = now;
        EmitSignal(nameof(Dashed));

        // update animation to dash immediately
        UpdateAnimationState();
    }

    /// <summary>
    /// Dash towards a world position and stop when near it (stopDistance). 
    /// If stopDistance == 0 the dash will run full duration.
    /// </summary>
    public void DashTowardsPosition(Vector2 worldPosition, float stopDistance = 0f)
    {
        if (!CanDash)
            return;

        var dir = worldPosition - GlobalPosition;
        if (dir.LengthSquared() <= 0.0001f)
            return;
        dir = dir.Normalized();

        _isDashing = true;
        _dashTimeLeft = _dashDuration;
        _dashVelocity = dir * (DashDistance / _dashDuration);
        _dashTargetPosition = worldPosition;
        if (stopDistance > 0f)
            DashStopDistance = stopDistance;
        _lastDashAt = (float)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        try { EmitSignal(nameof(Dashed)); } catch { }
        UpdateAnimationState();
    }

    /// <summary>
    /// Prepare to hold the attack animation at the specified hold position.
    /// </summary>
    public void PrepareAttackHold()
    {
        if (_animationPlayer == null || string.IsNullOrEmpty(AttackAnimationName))
            return;

        if (!_animationPlayer.HasAnimation(AttackAnimationName))
            return;

        // play then seek to hold position, then stop so the player is posed mid-attack
        var anim = _animationPlayer.GetAnimation(AttackAnimationName);
        if (anim == null)
            return;

        double len = anim.Length;
        double seek = Math.Max(0.0, Math.Min(1.0, AttackHoldPosition)) * len;
        _animationPlayer.Play(AttackAnimationName);
        _animationPlayer.Seek((float)seek, true);
        _animationPlayer.Stop();
        _isAttackHeld = true;
        _isAttacking = true;
        EmitSignal(nameof(AnimationStarted), AttackAnimationName);
    }

    /// <summary>
    /// Resume the attack animation from the held position.
    /// </summary>
    public void ResumeAttack()
    {
        if (!_isAttackHeld)
            return;

        _isAttackHeld = false;
        if (_animationPlayer == null || !_animationPlayer.HasAnimation(AttackAnimationName))
        {
            // nothing to play; clear state
            _isAttacking = false;
            EmitSignal(nameof(AnimationFinished), AttackAnimationName);
            return;
        }

        // continue playing the attack from the current position
        _animationPlayer.Play(AttackAnimationName);
    }

    public void PlayAttackAnimation()
    {
        _isAttacking = true;

        // controller will call PlayAnimation with blend
        UpdateAnimationState();
    }

    public void EquipRelic(string relicId)
    {
        if (_equippedRelics.Add(relicId))
            EmitSignal(nameof(RelicEquipped), relicId);
    }

    public void UnequipRelic(string relicId)
    {
        if (_equippedRelics.Remove(relicId))
            EmitSignal(nameof(RelicUnequipped), relicId);
    }

    public IReadOnlyCollection<string> GetEquippedRelics() => _equippedRelics;
}
