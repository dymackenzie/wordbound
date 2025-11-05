using Godot;
using System;

public partial class EnemyBase : Node2D
{
	[Export] public PackedScene TypingChallengeScene { get; set; }

	private TypingChallenge _typingChallenge;

	public override void _Ready()
	{
		base._Ready();

		InstantiateTypingChallenge();
	}

	private void InstantiateTypingChallenge()
	{
		if (TypingChallengeScene == null)
			return;

		_typingChallenge = TypingChallengeScene.Instantiate() as TypingChallenge;
		AddChild(_typingChallenge);

		string id = Guid.NewGuid().ToString();
        string placeholderText = ""; // the spawner/room should set the real text later
        double timeLimit = 6.0; // default time, can be overridden by the spawner or TypingManager
        _typingChallenge.Prepare(id, placeholderText, timeLimit);
	}

	public TypingChallenge GetTypingChallenge()
	{
		return _typingChallenge;
	}
}

