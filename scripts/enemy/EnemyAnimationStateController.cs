using Godot;
using System;

public class EnemyAnimationStateController
{
    private EnemyBase _owner;

    public double BlendSeconds { get; set; } = 0.08;
    public float MoveThreshold { get; set; } = 6.0f;

    public EnemyAnimationStateController(EnemyBase owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Update(Vector2 movement, bool isAttacking, bool isDead)
    {
        if (_owner == null)
            return;

        string target = _owner.IdleAnimationName;

        if (isDead && !string.IsNullOrEmpty(_owner.DeathAnimationName))
        {
            target = _owner.DeathAnimationName;
        }
        else if (isAttacking && !string.IsNullOrEmpty(_owner.AttackAnimationName))
        {
            target = _owner.AttackAnimationName;
        }
        else if (movement.Length() > MoveThreshold && !string.IsNullOrEmpty(_owner.MoveAnimationName))
        {
            target = _owner.MoveAnimationName;
        }
        else
        {
            target = _owner.IdleAnimationName;
        }

        if (string.IsNullOrEmpty(target))
            return;

        if (!_owner.IsPlayingAnimation(target))
            _owner.PlayAnimation(target, BlendSeconds);
    }
}
