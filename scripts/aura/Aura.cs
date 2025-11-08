using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Aura : Area2D
{
	[Signal] public delegate void EnemyEnteredAuraEventHandler(Node enemy);
	[Signal] public delegate void EnemyExitedAuraEventHandler(Node enemy);
	[Signal] public delegate void AuraActivationReadyEventHandler(bool hasEnemies);
	[Signal] public delegate void AuraActivatedEventHandler();
	[Signal] public delegate void AuraFailedEventHandler(Node attacker);
	[Signal] public delegate void EnemyPurifiedEventHandler(Node enemy);
	[Signal] public delegate void AuraEndedEventHandler(bool success);

	[Export] public NodePath PlayerPath = new();
	[Export] public float StopDashingDistance = 32f;

	private Player _player;

	// set of enemies currently within the aura
	private readonly HashSet<Node> _enemiesInAura = [];

	// queue of enemies to target, sorted by distance to player
	private readonly List<Node> _queue = [];

	private bool _isActive = false;

	private double _activeRemaining = 0.0;
	private TypingChallenge _activeChallenge = null;
	private Node _activeEnemy = null;

	public override void _Ready()
	{
		// connect Area2D signals
		Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
		Connect("body_exited", new Callable(this, nameof(OnBodyExited)));

		if (PlayerPath.ToString() != "")
		{
			Player _player = GetNodeOrNull(PlayerPath) as Player;
			_player.Connect("AuraActivatedEventHandler", new Callable(this, nameof(AttemptActivateAura)));
		}
		else
		{
			throw new Exception("Aura: PlayerPath not set; please assign in the editor.");
		}
	}

	public override void _Process(double delta)
	{
		// No per-frame timer logic here; the TypingChallenge is authoritative and
		// will emit Progress/Failed/Completed signals that Aura listens to.
	}

	private TypingChallenge FindTypingChallengeUnder(Node enemy)
	{
		if (enemy == null) return null;

		foreach (var child in enemy.GetChildren())
		{
			if (child is TypingChallenge tc)
				return tc;
		}
		return null;
	}

	private void WireToChallenge(Node enemy, TypingChallenge ch)
	{
		// disconnect from previous
		if (_activeChallenge != null)
		{
			try { _activeChallenge.Disconnect("Progress", new Callable(this, nameof(OnChallengeProgress))); } catch { }
			try { _activeChallenge.Disconnect("Failed", new Callable(this, nameof(OnChallengeFailedFromChallenge))); } catch { }
			try { _activeChallenge.Disconnect("Completed", new Callable(this, nameof(OnChallengeCompletedFromChallenge))); } catch { }
		}

		_activeChallenge = ch;
		_activeEnemy = enemy;
		_activeRemaining = 0.0;

		if (_activeChallenge == null)
			return;

		try { _activeChallenge.Connect("Progress", new Callable(this, nameof(OnChallengeProgress))); } catch { }
		try { _activeChallenge.Connect("Failed", new Callable(this, nameof(OnChallengeFailedFromChallenge))); } catch { }
		try { _activeChallenge.Connect("Completed", new Callable(this, nameof(OnChallengeCompletedFromChallenge))); } catch { }

		try { OneShotConnector.ConnectOnce(_player, nameof(Player.DashArrived), this, nameof(OnPlayerDashArrived)); } catch { }
		_player.DashTowardsPosition((enemy as Node2D).GlobalPosition, StopDashingDistance);
	}

	private void OnPlayerDashArrived()
	{
		try { _player.Disconnect(nameof(Player.DashArrived), new Callable(this, nameof(OnPlayerDashArrived))); } catch { }
		_player.PrepareAttackHold();
	}

	private void FollowNextInQueue()
	{
		// move to the next available challenge in the queue; if none, deactivate
		while (_queue.Count > 0)
		{
			var enemy = _queue[0];
			var ch = FindTypingChallengeUnder(enemy);
			if (ch != null)
			{
				WireToChallenge(enemy, ch);
				return;
			}

			// no challenge for this enemy, skip it
			_queue.RemoveAt(0);
		}

		// nothing left
		Deactivate(true);
	}

	private void OnBodyEntered(Node body)
	{
		if (body == null)
			return;

		if (!body.IsInGroup("Enemy"))
			return;

		if (_enemiesInAura.Add(body))
		{
			EmitSignal(nameof(EnemyEnteredAura), body);
			EmitSignal(nameof(AuraActivationReady), _enemiesInAura.Count > 0);

			if (_isActive)
			{
				InsertEnemyIntoQueueSorted(body);
				// if no active challenge yet, attempt to follow now
				if (_activeChallenge == null)
					FollowNextInQueue();
			}
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body == null)
			return;

		if (!body.IsInGroup("Enemy"))
			return;

		if (_enemiesInAura.Remove(body))
		{
			EmitSignal(nameof(EnemyExitedAura), body);
			EmitSignal(nameof(AuraActivationReady), _enemiesInAura.Count > 0);
			_queue.Remove(body);
		}
	}

	/// <summary>
	/// Attempts to activate the aura. If there are no enemies in the aura,
	/// the activation will fail.
	/// </summary>
	public void AttemptActivateAura()
	{
		if (_isActive)
			return;

		if (_enemiesInAura.Count == 0)
		{
			EmitSignal(nameof(AuraActivationReady), false);
			return;
		}

		Vector2 playerPos = _player.GlobalPosition;
		_queue.Clear();
		// sort enemies by distance to player (nearest first)
		_queue.AddRange(_enemiesInAura.OrderBy(e => Utility.DistanceToNode(e, playerPos)));

		_isActive = true;

		CallCameraMethod("StartKillCinematic");
		EmitSignal(nameof(AuraActivated));
		FollowNextInQueue();
	}

	private void InsertEnemyIntoQueueSorted(Node enemy)
	{
		Vector2 playerPos = _player.GlobalPosition;
		double d = Utility.DistanceToNode(enemy, playerPos);
		int idx = _queue.FindIndex(e => Utility.DistanceToNode(e, playerPos) > d);
		if (idx < 0)
			_queue.Add(enemy);
		else
			_queue.Insert(idx, enemy);
	}

	/// <summary>
    /// Handles when the active enemy's challenge is completed.
    /// </summary>
	public void OnChallengeCompleted(Node enemy)
	{
		if (enemy == null)
			return;

		_queue.Remove(enemy);
		_enemiesInAura.Remove(enemy);

		EmitSignal(nameof(EnemyPurified), enemy);

		// if this was the active enemy, clear and follow next
		if (_activeEnemy == enemy)
		{
			_activeEnemy = null;
			_activeChallenge = null;
			FollowNextInQueue();
		}

		if (_queue.Count == 0 && _activeChallenge == null)
			Deactivate(true);
	}

	// handlers for signals coming directly from a TypingChallenge instance
	private void OnChallengeProgress(string challengeId, string typed, double remainingTime)
	{
		_activeRemaining = remainingTime;
	}

	private void OnChallengeFailedFromChallenge(string challengeId, string reason)
	{
		// map active challenge -> enemy and forward
		EmitSignal(nameof(AuraFailed), _activeEnemy);
		Deactivate(false);
	}

	private void OnChallengeCompletedFromChallenge(string challengeId, string finalBuffer, double timeLeft)
	{
		try { OneShotConnector.ConnectOnce(_player, nameof(Player.AnimationFinished), this, nameof(OnPlayerAttackFinished)); } catch { }
		try
		{
			_player.ResumeAttack();
		}
		catch
		{
			_player.PlayAnimation(_player.AttackAnimationName);
		}
	}

	private void OnPlayerAttackFinished(string animName)
	{
		try { _player.Disconnect(nameof(Player.AnimationFinished), new Callable(this, nameof(OnPlayerAttackFinished))); } catch { }
		OnChallengeCompleted(_activeEnemy);
	}

	private void Deactivate(bool success)
	{
		_player.ResumeAttack();
		_isActive = false;
		_queue.Clear();
		CallCameraMethod("EndKillCinematic");
		EmitSignal(nameof(AuraEnded), success);
	}

	private void CallCameraMethod(string methodName)
	{
		var cam = GetViewport().GetCamera2D();
		if (cam == null)
			return;
		try { cam.Call(methodName); } catch { }
	}

	/// <summary>
    /// Returns whether the aura is currently active.
    /// </summary>
	public bool IsActive => _isActive;

	/// <summary>
	/// Returns the most recent remaining time reported by the active TypingChallenge.
	/// </summary>
	public double GetRemainingTimeSeconds()
	{
		if (!_isActive || _activeChallenge == null)
			return 0.0;
		return _activeRemaining;
	}

	/// <summary>
	/// Returns the maximum time allowed for the active TypingChallenge.
	/// </summary>
	public double GetMaxAuraTimeSeconds()
	{
		if (_activeChallenge != null)
			return _activeChallenge.MaxRemainingSeconds;
		return 0.0;
	}
}

