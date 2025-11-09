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
    [Signal] public delegate void PlayerDiedEventHandler();
    
    [Export] public float Health { get; set; } = 10f;
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
    [Export] public string DamageAnimationName { get; set; } = "damage";
    [Export] public string DeathAnimationName { get; set; } = "death";
    [Export] public double AnimationBlendSeconds { get; set; } = 0.12;
    [Export] public float RunAnimationThreshold { get; set; } = 6.0f; // velocity magnitude to consider running
    [Export] public float AttackHoldPosition { get; set; } = 0.5f; // normalized 0..1 of animation length to hold at

    [Export] public PackedScene GhostScene { get; set; } = null;
    [Export] public bool GhostEnabled { get; set; } = true;
    [Export] public float GhostSpawnInterval { get; set; } = 0.05f;
    [Export] public float GhostLifetimeOverride { get; set; } = 0.0f; // 0 = use ghost's default
    [Export] public float GhostInitialOpacity { get; set; } = 0.6f;

    private float _lastDashAt = -999f; // seconds (OS ticks)
    private const float _dashDuration = 0.16f;
    private DashController _dashController = null;

    private readonly HashSet<string> _equippedRelics = [];

    private AnimationPlayer _animationPlayer = null;
    private AnimationStateController _animController = null;
    private string _currentAnimation = "";
    private bool _isAttacking = false;
    private bool _isAttackHeld = false;
    private bool _isDamaged = false;
    private bool _isDead = false;

    public AnimationState CurrentAnimationState { get; set; } = AnimationState.Idle;

    public override void _Ready()
    {
        _animationPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
        _animationPlayer.Connect("animation_finished", new Callable(this, nameof(OnAnimationPlayerFinished)));
        InitAnimationController();
        InitDashController();
    }
    
    private void InitDashController()
    {
        _dashController = new DashController
        {
            DashDistance = DashDistance,
            DashDuration = _dashDuration,
            DashStopDistance = DashStopDistance
        };
        _dashController.OnDashed += () => { try { EmitSignal(nameof(Dashed)); } catch { } };
        _dashController.OnDashArrived += () => { try { EmitSignal(nameof(DashArrived)); } catch { }; PrepareAttackHold(); };
        _dashController.OnDashEnded += () => { };

        // configure ghost behaviour on controller
        _dashController.GhostScene = GhostScene;
        _dashController.GhostEnabled = GhostEnabled;
        _dashController.GhostLifetimeOverride = GhostLifetimeOverride;
        _dashController.GhostInitialOpacity = GhostInitialOpacity;
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
        bool isDashing = _dashController?.IsDashing ?? false;
        _animController?.Update(Velocity, isDashing, _isAttacking, _isDamaged, _isDead);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_dashController != null && _dashController.Update(this, delta))
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
        _currentAnimation = "";

        if (animName == AttackAnimationName)
            _isAttacking = false;
        if (animName == DamageAnimationName)
            _isDamaged = false;
        if (animName == DeathAnimationName)
        {
            _isDead = true;
            try { EmitSignal(nameof(PlayerDiedEventHandler)); } catch { }
        }
        EmitSignal(nameof(AnimationFinished), animName);
    }

    /// <summary>
    /// Plays the specified animation with optional blend time. Will start and finish signals so that
    /// callers can rely on them.
    /// </summary>
    public void PlayAnimation(string animName, double blend = 0.12)
    {
        EmitSignal(nameof(AnimationStarted), animName);

        if (string.IsNullOrEmpty(animName) || !_animationPlayer.HasAnimation(animName))
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

    /// <summary>
    /// Initiates a dash in the direction of current input, if any.
    /// </summary>
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

        _lastDashAt = now;
        _dashController?.StartDash(this, input, DashDistance, _dashDuration);
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

        _lastDashAt = (float)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        _dashController?.StartDashTowards(this, worldPosition, DashDistance, _dashDuration, stopDistance);
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

    public void PlayDamageAnimation()
    {
        _isDamaged = true;
        if (_animationPlayer.HasAnimation(DamageAnimationName))
            _animationPlayer.Play(DamageAnimationName, customBlend: AnimationBlendSeconds);
    }

    public void PlayDeathAnimation()
    {
        _isDead = true;
        if (_animationPlayer.HasAnimation(DeathAnimationName))
            _animationPlayer.Play(DeathAnimationName, customBlend: AnimationBlendSeconds);
    }

    /// <summary>
    /// Called by enemies via Call("TakeDamage", amount).
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_isDead)
            return;
        Health -= amount;
        try { PlayDamageAnimation(); } catch { }
        if (Health <= 0f)
        {
            try { PlayDeathAnimation(); } catch { }
        }
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
