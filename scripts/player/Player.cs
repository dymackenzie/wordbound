using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [Signal] public delegate void AuraActivatedEventHandler();
    [Signal] public delegate void DashedEventHandler();
    [Signal] public delegate void RelicEquippedEventHandler(string relicId);
    [Signal] public delegate void RelicUnequippedEventHandler(string relicId);
    [Signal] public delegate void AnimationStartedEventHandler(string animationName);
    [Signal] public delegate void AnimationFinishedEventHandler(string animationName);

    [Export] public float Speed { get; set; } = 220f;
    [Export] public float DashDistance { get; set; } = 180f;
    [Export] public float DashCooldown { get; set; } = 1.0f;
    [Export] public float DashInvulnerabilityTime { get; set; } = 0.12f;
    [Export] public bool CanDash { get; set; } = false;

    [Export] public NodePath AnimationPlayerPath { get; set; } = new NodePath("AnimationPlayer");
    [Export] public string IdleAnimationName { get; set; } = "idle";
    [Export] public string RunAnimationName { get; set; } = "run";
    [Export] public string DashAnimationName { get; set; } = "dash";
    [Export] public string AttackAnimationName { get; set; } = "attack";
    [Export] public double AnimationBlendSeconds { get; set; } = 0.12;
    [Export] public float RunAnimationThreshold { get; set; } = 6.0f; // velocity magnitude to consider running

    private float _lastDashAt = -999f; // seconds (OS ticks)
    private bool _isDashing = false;
    private Vector2 _dashVelocity = Vector2.Zero;
    private float _dashTimeLeft = 0f;
    private const float _dashDuration = 0.16f;

    private readonly HashSet<string> _equippedRelics = [];

    private AnimationPlayer _animationPlayer = null;
    private AnimationStateController _animController = null;
    private string _currentAnimation = "";
    private bool _isAttacking = false;

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
        if (_isDashing)
        {
            _dashTimeLeft -= (float)delta;
            Velocity = _dashVelocity;
            MoveAndSlide();
            if (_dashTimeLeft <= 0f)
                _isDashing = false;
            return;
        }

        var input = new Vector2(
            Input.GetActionStrength("d_right") - Input.GetActionStrength("a_left"),
            Input.GetActionStrength("s_down") - Input.GetActionStrength("w_up")
        );

        if (input.LengthSquared() > 0f)
            input = input.Normalized();

        Velocity = input * Speed;
        MoveAndSlide();
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
