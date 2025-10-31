using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [Signal] public delegate void aura_activation_ready(bool has_enemies);
    [Signal] public delegate void aura_activated();
    [Signal] public delegate void request_kill_zone(Node2D enemy);
    [Signal] public delegate void dashed();
    [Signal] public delegate void relic_equipped(string relic_id);
    [Signal] public delegate void relic_unequipped(string relic_id);

    [Export] public float Speed { get; set; } = 220f;
    [Export] public float DashDistance { get; set; } = 180f;
    [Export] public float DashCooldown { get; set; } = 1.0f;
    [Export] public float DashInvulnerabilityTime { get; set; } = 0.12f;
    [Export] public bool CanDash { get; set; } = false;

    private Area2D _aura;
    private readonly List<Node2D> _enemiesInAura = [];

    private float _lastDashAt = -999f; // seconds (OS ticks)
    private bool _isDashing = false;
    private Vector2 _dashVelocity = Vector2.Zero;
    private float _dashTimeLeft = 0f;
    private const float _dashDuration = 0.16f;

    private readonly HashSet<string> _equippedRelics = [];

    public override void _Ready()
    {
        // find Aura child
        _aura = GetNodeOrNull<Area2D>("Aura");
        if (_aura != null)
        {
            _aura.BodyEntered += OnAuraBodyEntered;
            _aura.BodyExited += OnAuraBodyExited;
        }
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
        if (_Input.IsActionJustPressed("activate_aura"))
        {
            AttemptActivateAura();
        }
        if (_Input.IsActionJustPressed("dash"))
        {
            Dash();
        }
    }

    private void OnAuraBodyEntered(Node body)
    {
        if (body is Node2D n)
        {
            if (!_enemiesInAura.Contains(n))
                _enemiesInAura.Add(n);
            EmitSignal(nameof(aura_activation_ready), true);
        }
    }

    private void OnAuraBodyExited(Node body)
    {
        if (body is Node2D n)
        {
            _enemiesInAura.Remove(n);
            EmitSignal(nameof(aura_activation_ready), _enemiesInAura.Count > 0);
        }
    }

    public void AttemptActivateAura()
    {
        if (_enemiesInAura.Count == 0)
        {
            EmitSignal(nameof(aura_activation_ready), false);
            return;
        }

        var target = FindNearestEnemy();
        if (target != null)
        {
            EmitSignal(nameof(aura_activated));
            EmitSignal(nameof(request_kill_zone), target);
        }
    }

    private Node2D FindNearestEnemy()
    {
        Node2D best = null;
        float bestDist = float.MaxValue;
        foreach (var enemy in _enemiesInAura)
        {
            if (enemy == null) continue;
            var enemyDistance = enemy.GlobalPosition.DistanceTo(GlobalPosition);
            if (enemyDistance < bestDist)
            {
                bestDist = enemyDistance;
                best = enemy;
            }
        }
        return best;
    }

    public void Dash()
    {
        if (!CanDash)
            return;

        var now = OS.GetTicksMsec() / 1000.0f;
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
        _lastDashAt = (float)now;
        EmitSignal(nameof(dashed));
    }

    public void EquipRelic(string relicId)
    {
        if (_equippedRelics.Add(relicId))
            EmitSignal(nameof(relic_equipped), relicId);
    }

    public void UnequipRelic(string relicId)
    {
        if (_equippedRelics.Remove(relicId))
            EmitSignal(nameof(relic_unequipped), relicId);
    }

    public IReadOnlyCollection<string> GetEquippedRelics() => _equippedRelics;
}
