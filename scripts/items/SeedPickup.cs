using Godot;
using System;

public partial class SeedPickup : Sprite2D
{
    [Export] public int Value { get; set; } = 1;
    [Export] public float ScatterSpeed { get; set; } = 120f;
    [Export] public float InitialVerticalSpeed { get; set; } = 140f;
    [Export] public float Gravity { get; set; } = 600f;
    
    private GameState _gameState = null;

    private Vector2 _velocity = Vector2.Zero;
    private Vector2 _basePosition = Vector2.Zero; // horizontal (ground) position
    private double _age = 0.0;
    private RandomNumberGenerator _rng = new();

    private float _z = 0f;    // height above ground in pixels
    private float _vz = 0f;   // vertical velocity (pixels/sec)

    public override void _Ready()
    {
        _rng.Randomize();
        
        var angle = (float)_rng.RandfRange(0f, (float)Math.PI * 2f);
        _velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * ScatterSpeed;

        _basePosition = GlobalPosition;

        _z = _rng.RandfRange(6f, 14f);
        _vz = _rng.RandfRange(InitialVerticalSpeed * 0.6f, InitialVerticalSpeed);

        GetNodeOrNull<Area2D>("PickupArea")?.Connect("body_entered", new Callable(this, nameof(OnPickupAreaBodyEntered)));
    }

    public override void _PhysicsProcess(double delta)
    {
        _age += delta;
        _vz -= Gravity * (float)delta;
        _z += _vz * (float)delta;

        if (_z <= 0f && _vz <= 0f)
        {
            _z = 0f;
            _vz = 0f;
            _velocity = Vector2.Zero;
        }

        if (Math.Abs(_vz) > 0.001f)
        {
            _basePosition += _velocity * (float)delta;
        }

        if (Math.Abs(_vz) <= 0.001f && _age > 10.0)
            QueueFree();

        // apply vertical offset to render position so the sprite appears above the ground
        GlobalPosition = _basePosition + new Vector2(0f, -_z);

        float heightFactor = Mathf.Clamp(1f - (_z / 120f), 0.6f, 1f);
        Scale = new Vector2(heightFactor, heightFactor);

        var shadow = GetNodeOrNull<Sprite2D>("Shadow");
        if (shadow != null)
        {
            shadow.GlobalPosition = _basePosition;
            float s = Mathf.Clamp(1f - (_z / 180f), 0.35f, 1f);
            shadow.Scale = new Vector2(s, s);
            var c = shadow.Modulate;
            shadow.Modulate = new Color(c.R, c.G, c.B, Mathf.Clamp(1f - (_z / 160f), 0.25f, 1f));
        }
    }

    private void Pickup()
    {
        _gameState ??= GetTree().Root.GetNodeOrNull<GameState>("GameState");
        _gameState?.AddSeeds(Value);
        QueueFree();
    }

    private void OnPickupAreaBodyEntered(Node body)
    {
        // only allow pickup when seed has come to rest
        if (Math.Abs(_vz) > 0.001f)
            return;

        if (body is not Player player)
            return;

        player.OnSeedPickedUp();
        Pickup();
    }
}
