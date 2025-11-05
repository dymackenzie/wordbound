using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [Signal] public delegate void AuraActivatedEventHandler();
    [Signal] public delegate void DashedEventHandler();
    [Signal] public delegate void RelicEquippedEventHandler(string relicId);
    [Signal] public delegate void RelicUnequippedEventHandler(string relicId);

    [Export] public float Speed { get; set; } = 220f;
    [Export] public float DashDistance { get; set; } = 180f;
    [Export] public float DashCooldown { get; set; } = 1.0f;
    [Export] public float DashInvulnerabilityTime { get; set; } = 0.12f;
    [Export] public bool CanDash { get; set; } = false;

    private float _lastDashAt = -999f; // seconds (OS ticks)
    private bool _isDashing = false;
    private Vector2 _dashVelocity = Vector2.Zero;
    private float _dashTimeLeft = 0f;
    private const float _dashDuration = 0.16f;

    private readonly HashSet<string> _equippedRelics = [];

    public override void _Ready()
    {
        
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
        // use the global Input helper so UI can consume/block events as needed
        if (Input.IsActionJustPressed("activate_aura"))
        {
            EmitSignal(nameof(AuraActivated));
        }
        if (Input.IsActionJustPressed("dash"))
        {
            Dash();
        }
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
