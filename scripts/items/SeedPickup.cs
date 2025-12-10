using Godot;
using System;

public partial class SeedPickup : Sprite2D
{
    [Export] public int Value { get; set; } = 1;
    [Export] public float ScatterSpeed { get; set; } = 120f;
    [Export] public float ScatterDuration { get; set; } = 0.4f; // seconds of free scatter before homing
    [Export] public float HomingSpeed { get; set; } = 220f;
    [Export] public float PickupRadius { get; set; } = 16f;
    
    private Player _player = null;
    private GameState _gameState = null;

    private Vector2 _velocity = Vector2.Zero;
    private double _age = 0.0;
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        
        var angle = (float)_rng.RandfRange(0f, (float)Math.PI * 2f);
        _velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * ScatterSpeed;

        CacheNodes();
    }

    private void CacheNodes()
    {
        _gameState ??= GetTree().Root.GetNodeOrNull<GameState>("GameState");

        var nodes = GetTree().GetNodesInGroup("Player");
        if (nodes.Count > 0)
            _player = nodes[0] as Player;
    }

    public override void _PhysicsProcess(double delta)
    {
        _age += delta;

        if (_age < ScatterDuration)
        {
            // scatter movement with light damping
            GlobalPosition += _velocity * (float)delta;
            _velocity = _velocity.MoveToward(Vector2.Zero, 2f * (float)delta);
            return;
        }

        if (_player != null)
        {
            var toPlayer = _player.GlobalPosition - GlobalPosition;
            var dist = toPlayer.Length();
            if (dist <= PickupRadius)
            {
                Pickup();
                return;
            }

            var dir = toPlayer.Normalized();
            GlobalPosition += dir * HomingSpeed * (float)delta;
        }
        else
        {
            // no player found — slowly decay and remove after time
            _age += delta;
            if (_age > 10.0) QueueFree();
        }
    }

    private void Pickup()
    {
        _gameState.AddSeeds(Value);
        _player.OnSeedPickedUp();
        QueueFree();
    }
}
