using Godot;
using System;

/// <summary>
/// Animation states for the player character.
/// Higher numbers indicate higher priority.
/// </summary>
public enum AnimationState
{
    Idle = 0,
    Run = 1,
    Dash = 2,
    Attack = 3,
    Damage = 4,
    Death = 5,
}

public class AnimationStateController
{
    private readonly Player _player;
    private AnimationState _current = AnimationState.Idle;

    public double BlendSeconds { get; set; } = 0.12;
    public float RunThreshold { get; set; } = 0.1f;

    public AnimationStateController(Player player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
    }

    /// <summary>
    /// Decide which animation to play based on velocity and flags.
    /// </summary>
    public void Update(object velocityObj, bool isDashing, bool isAttacking, bool isDamaged = false, bool isDead = false)
    {
        Vector2 velocity;
        try
        {
            if (velocityObj is Vector2 v)
                velocity = v;
            else
                velocity = (Vector2)velocityObj;
        }
        catch { velocity = Vector2.Zero; }

        AnimationState desiredState;
        if (isDead)
            desiredState = AnimationState.Death;
        else if (isDamaged)
            desiredState = AnimationState.Damage;
        else if (isAttacking)
            desiredState = AnimationState.Attack;
        else if (isDashing)
            desiredState = AnimationState.Dash;
        else if (velocity.Length() > RunThreshold)
            desiredState = AnimationState.Run;
        else
            desiredState = AnimationState.Idle;

        if (desiredState == _current)
            return;

        _current = desiredState;

        string animName;
        animName = _current switch
        {
            AnimationState.Death => _player.DeathAnimationName,
            AnimationState.Damage => _player.DamageAnimationName,
            AnimationState.Attack => _player.AttackAnimationName,
            AnimationState.Dash => _player.DashAnimationName,
            AnimationState.Run => _player.RunAnimationName,
            _ => _player.IdleAnimationName,
        };

        if (!string.IsNullOrEmpty(animName))
            _player.PlayAnimation(animName, BlendSeconds);

        // expose current state on player
        _player.CurrentAnimationState = _current;
    }
}
