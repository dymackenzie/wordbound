using Godot;
using System;

public partial class ProjectileEnemy : EnemyBase
{
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public float FireInterval { get; set; } = 1.2f;
    [Export] public float ProjectileSpeed { get; set; } = 300f;
    [Export] public float ProjectileDamage { get; set; } = 1.0f;
    [Export] public bool FireDuringOrbit { get; set; } = true;
    [Export] public bool FireDuringApproach { get; set; } = false;

    private float _fireTimer = 0f;
    private float _savedMoveSpeed = 0f;
    private bool _isMovementSuppressed = false;

    public override void _Ready()
    {
        base._Ready();
        _fireTimer = 0f;
        _savedMoveSpeed = MoveSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        bool shouldFireNow = ShouldSuppressMovement();

        // note: movement may be suppressed above
        base._PhysicsProcess(delta);

        if (_playerNode == null)
            return;

        if (!shouldFireNow)
        {
            _fireTimer = 0f;
            return;
        }

        _fireTimer += (float)delta;
        if (_fireTimer >= FireInterval)
        {
            _fireTimer = 0f;
            SpawnProjectileAtPlayer();
        }
    }

    private bool ShouldSuppressMovement()
    {
        bool shouldFireNow = (_state == EnemyState.Orbit && FireDuringOrbit) || (_state == EnemyState.Approach && FireDuringApproach);

        if (shouldFireNow && !_isMovementSuppressed)
        {
            _savedMoveSpeed = MoveSpeed;
            MoveSpeed = 0f;
            _isMovementSuppressed = true;
        }
        else if (!shouldFireNow && _isMovementSuppressed)
        {
            MoveSpeed = _savedMoveSpeed;
            _isMovementSuppressed = false;
        }

        return shouldFireNow;
    }

    private void SpawnProjectileAtPlayer()
    {
        if (_playerNode == null)
            return;

        Vector2 dir = (_playerNode.GlobalPosition - GlobalPosition).Normalized();

        Projectile proj = null;
        if (ProjectileScene != null)
        {
            var inst = ProjectileScene.Instantiate();
            proj = inst as Projectile;
            if (proj == null && inst is Node nodeInst)
            {
                proj = nodeInst as Projectile;
            }
        }

        AddChild(proj);
        proj.GlobalPosition = GlobalPosition;
        proj.Initialize(dir, ProjectileSpeed, ProjectileDamage);
    }
}
