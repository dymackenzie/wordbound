using Godot;
using System;

public abstract partial class EnemyBase : CharacterBody2D
{
	[Signal] public delegate void PurifiedEventHandler(Node enemy);
	[Signal] public delegate void DissolvedEventHandler(Node enemy, Node player);
	[Signal] public delegate void DamagedEventHandler(double newHealth);
	[Signal] public delegate void AnimationStartedEventHandler(string animationName);
	[Signal] public delegate void AnimationFinishedEventHandler(string animationName);

	[Export] public PackedScene TypingChallengeScene { get; set; }

	[Export] public NodePath AnimationPlayerPath { get; set; } = new NodePath("AnimationPlayer");
	[Export] public string IdleAnimationName { get; set; } = "idle";
	[Export] public string MoveAnimationName { get; set; } = "move";
	[Export] public string AttackAnimationName { get; set; } = "attack";
	[Export] public string DeathAnimationName { get; set; } = "death";
	[Export] public double AnimationBlendSeconds { get; set; } = 0.08;

	[Export] public float MoveSpeed { get; set; } = 120f;
	[Export] public float OrbitSpeed { get; set; } = 2.0f; // radians/sec when circling
	[Export] public float SurroundDistance { get; set; } = 120f; // desired distance to player when surrounding
	[Export] public float SurroundTolerance { get; set; } = 6f; // how close to desired surround position before switching to orbit
	[Export] public float SurroundDuration { get; set; } = 1.0f; // seconds to spend moving to the surround position before forcing orbit
	[Export] public float OrbitDuration { get; set; } = 2.0f; // seconds to orbit before aggressively approaching
	[Export] public float AttackRange { get; set; } = 20f; // distance at which the enemy dissolves into the player
	[Export] public float DamageOnDissolve { get; set; } = 1.0f;
	[Export] public float Health { get; set; } = 1.0f;

	[Export] public int SeedReward { get; set; } = 1;
	[Export] public PackedScene SeedPickupScene { get; set; } = null;

	protected TypingChallenge _typingChallenge;
	protected Node2D _playerNode;

	private float _orbitAngle = 0f;
	private float _surroundTimer = 0f;
    private float _orbitTimer = 0f;
    private float _attackTimer = 0f;

	private AnimationPlayer _animationPlayer = null;
	private string _currentAnimation = "";

	private EnemyAnimationStateController _animController = null;
	private bool _isAttacking = false;
	private bool _isDead = false;

	protected enum EnemyState
	{
		Surround,
		Orbit,
		Approach
	}
	protected EnemyState _state = EnemyState.Surround;

    public override void _Ready()
    {
        base._Ready();

		LocatePlayer();
		InstantiateTypingChallenge();

		_animationPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
		if (_animationPlayer != null)
		{
			try { _animationPlayer.Connect("animation_finished", new Callable(this, nameof(OnAnimationPlayerFinished))); } catch { }
		}

		// animation controller
		_animController = new EnemyAnimationStateController(this)
		{
			BlendSeconds = AnimationBlendSeconds,
			MoveThreshold = 6.0f
		};
    }
    
    private void LocatePlayer()
    {
        if (_playerNode == null)
        {
            var list = GetTree().GetNodesInGroup("Player");
            if (list.Count > 0)
                _playerNode = list[0] as Player;
        }
    }

    private void InstantiateTypingChallenge()
    {
        if (TypingChallengeScene == null)
            return;

        _typingChallenge = TypingChallengeScene.Instantiate() as TypingChallenge;
        ConfigureTypingChallenge(Guid.NewGuid().ToString(), _typingChallenge);
        AddChild(_typingChallenge);

		// connect to typing challenge signals
		_typingChallenge.Connect("Completed", new Callable(this, nameof(OnTypingChallengeCompleted)));
		_typingChallenge.Connect("Failed", new Callable(this, nameof(OnTypingChallengeFailed)));
    }

	public void PlayAnimation(string animName, double blend = 0.08)
	{
		EmitSignal(nameof(AnimationStarted), animName);
		_currentAnimation = animName;
		_animationPlayer.Play(animName, customBlend: blend);
	}

	public void StopAnimation()
	{
		_animationPlayer?.Stop();
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

	private void OnAnimationPlayerFinished(string animName)
	{
		_currentAnimation = string.Empty;
		EmitSignal(nameof(AnimationFinished), animName);
		try { OnAnimationFinished(animName); } catch { }
		if (animName == AttackAnimationName)
			_isAttacking = false;
		if (animName == DeathAnimationName)
			_isDead = true;
	}

	public void PlayIdleAnimation() => PlayAnimation(IdleAnimationName, AnimationBlendSeconds);
	public void PlayMoveAnimation() => PlayAnimation(MoveAnimationName, AnimationBlendSeconds);
	public void PlayAttackAnimation()
	{
		_isAttacking = true;
		PlayAnimation(AttackAnimationName, AnimationBlendSeconds);
	}
	public void PlayDeathAnimation()
	{
		_isDead = true;
		PlayAnimation(DeathAnimationName, AnimationBlendSeconds);
	}

	/// <summary>
	/// Override in derived classes to react to animation completion events.
	/// </summary>
	protected virtual void OnAnimationFinished(string animName) { }

	public virtual TypingChallenge GetTypingChallenge()
	{
		return _typingChallenge;
	}

    private void UpdateStateTimers(double delta)
    {
        switch (_state)
        {
            case EnemyState.Surround:
                _surroundTimer += (float)delta;
                break;
            case EnemyState.Orbit:
                _orbitTimer += (float)delta;
                break;
            case EnemyState.Approach:
                break;
        }
    }

	private Vector2 HandleSurround(double delta, Vector2 toPlayer)
	{
		Vector2 desiredPos = _playerNode.GlobalPosition + toPlayer.Normalized() * SurroundDistance;
		Vector2 toDesired = desiredPos - GlobalPosition;
		Vector2 move = toDesired.Length() > 0.01f ? toDesired.Normalized() : Vector2.Zero;

		// if close enough or timed out, start orbiting
		if (toDesired.Length() <= SurroundTolerance || _surroundTimer >= SurroundDuration)
		{
			_state = EnemyState.Orbit;
			_orbitTimer = 0f;
			// initialize orbit angle to current angle around player to make transition smooth
			Vector2 rel = GlobalPosition - _playerNode.GlobalPosition;
			_orbitAngle = (float)Math.Atan2(rel.Y, rel.X);
		}

		return move;
	}

    private Vector2 HandleOrbit(double delta)
    {
        _orbitAngle += OrbitSpeed * (float)delta;
        Vector2 orbitPos = _playerNode.GlobalPosition + new Vector2((float)Math.Cos(_orbitAngle), (float)Math.Sin(_orbitAngle)) * SurroundDistance;
        Vector2 toOrbit = orbitPos - GlobalPosition;
        Vector2 move = toOrbit.Length() > 0.01f ? toOrbit.Normalized() : Vector2.Zero;

        if (_orbitTimer >= OrbitDuration)
        {
            _state = EnemyState.Approach;
        }

        return move;
    }

	private Vector2 HandleApproach(Vector2 toPlayer)
	{
		return toPlayer.Normalized();
	}

	// physics movement: approach, surround (orbit) and attack
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		_attackTimer = Math.Max(0f, _attackTimer - (float)delta);

		// state timers
		UpdateStateTimers(delta);

		if (_playerNode == null)
			return;

		Vector2 toPlayer = _playerNode.GlobalPosition - GlobalPosition;
		float dist = toPlayer.Length();

		// if within attack range, perform dissolve/attack
		if (dist <= AttackRange)
		{
            OnReachPlayer();
		}

		Vector2 move = Vector2.Zero;

		switch (_state)
		{
			case EnemyState.Surround:
				move = HandleSurround(delta, toPlayer);
				break;
			case EnemyState.Orbit:
				move = HandleOrbit(delta);
				break;
			case EnemyState.Approach:
				move = HandleApproach(toPlayer);
				break;
		}


		// update animation state based on movement and flags
		try
		{
			_animController?.Update(move * MoveSpeed, _isAttacking, _isDead || Health <= 0f);
		}
		catch { }

		// apply movement
		var velocity = move * MoveSpeed;
		GlobalPosition += velocity * (float)delta;
	}

	protected virtual void OnReachPlayer()
	{
		if (_playerNode != null)
		{
			try { _playerNode.Call("TakeDamage", DamageOnDissolve); } catch { }
			EmitSignal(nameof(Dissolved), this, _playerNode);
		}

        // default behavior: remove self (dissolve)
        Die();
	}

	private void OnTypingChallengeCompleted(string challengeId, string finalBuffer, double timeLeft)
	{
		EmitSignal(nameof(Purified), this);
		OnPurified();
		Die();
	}

	private void OnTypingChallengeFailed(string challengeId, string reason)
	{
		OnChallengeFailed(reason);
	}

	/// <summary>
	/// Pulls from the word pool the appropriate word based off complexity.
	/// </summary>
	public abstract string GenerateChallengeText();

	/// <summary>
    /// Calculates the time limit based off user wpm and word length.
    /// </summary>
	public abstract double GenerateTimeLimit(string word);

	public void ConfigureTypingChallenge(string id, TypingChallenge challenge)
	{
		if (challenge == null)
			return;

		string placeholderText = GenerateChallengeText();
		double timeLimit = GenerateTimeLimit(placeholderText);
		challenge.Prepare(id, placeholderText, timeLimit);
	}

    // hooks for derived classes
    protected virtual void OnPurified()
    {
		ApplyDamage(Health);
    }
    
	protected virtual void OnChallengeFailed(string reason) { }

	// allow external code to apply damage
	public virtual void ApplyDamage(float amount)
	{
		Health -= amount;
		try { EmitSignal(nameof(Damaged), Health); } catch { }
		if (Health <= 0f)
			Die();
	}

	protected virtual void Die()
	{
		var relicMgr = _playerNode.GetNodeOrNull<RelicManager>("RelicManager");
		int extra = relicMgr.GetKillSeedBonus(this);
		relicMgr.NotifyKill();

		SpawnSeedDrops(SeedReward + extra);

		QueueFree();
	}

	/// <summary>
	/// Spawns `count` seed pickups that scatter and then home to the player.
	/// </summary>
	protected void SpawnSeedDrops(int count)
	{
		if (count <= 0) return;
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		for (int i = 0; i < count; i++)
		{
			Sprite2D seedNode = SeedPickupScene.Instantiate() as Sprite2D;

			var offset = new Vector2((float)rng.RandfRange(-8f, 8f), (float)rng.RandfRange(-8f, 8f));
			seedNode.GlobalPosition = GlobalPosition + offset;

			// add to same parent as enemy so coordinate space is consistent
			if (GetParent() != null)
				GetParent().AddChild(seedNode);
			else
				GetTree().Root.AddChild(seedNode);
		}
	}
}

