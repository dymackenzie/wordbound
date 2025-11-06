using Godot;
using System;

public enum AnimationState
{
    Idle = 0,
    Run = 1,
    Dash = 2,
    Attack = 3,
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
    public void Update(object velocityObj, bool isDashing, bool isAttacking)
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

        // determine desired animation state
        AnimationState desiredState;
        if (isAttacking)
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

        // map enum to animation name
        string animName = _current switch
        {
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
