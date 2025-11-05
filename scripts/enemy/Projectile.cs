using Godot;
using System;

public partial class Projectile : CharacterBody2D
{
    [Export] public float Speed { get; set; } = 300f;
    [Export] public float Damage { get; set; } = 1.0f;
    [Export] public float Lifetime { get; set; } = 5.0f;

    private Vector2 _velocity = Vector2.Zero;
    private float _life = 0f;

    public void Initialize(Vector2 direction, float speed, float damage)
    {
        _velocity = direction.Normalized() * speed;
        Speed = speed;
        Damage = damage;
        _life = Lifetime;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _life -= (float)delta;
        if (_life <= 0f)
        {
            QueueFree();
            return;
        }

        var motion = _velocity * (float)delta;
        var collision = MoveAndCollide(motion);
        if (collision != null)
        {
            var obj = collision.GetCollider() as Node;
            if (obj != null)
            {
                try { obj.Call("TakeDamage", Damage); } catch { }
            }
            QueueFree();
            return;
        }
    }
}
