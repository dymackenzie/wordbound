using Godot;
using System;
using System.Text;
using System.Collections.Generic;

public partial class TypingManager : Node
{
	[Signal] public delegate void CharacterTypedEventHandler(string ch);
	[Signal] public delegate void MistypedEventHandler(Godot.Collections.Dictionary info);
	[Signal] public delegate void WordCompletedEventHandler(string challenge_id, Godot.Collections.Dictionary result);

    [Export] public int MaxBufferLength { get; set; } = 256;
	[Export] public int MaxQueueLength { get; set; } = 16;

	private Node _activeChallenge;

	private const int DefaultBufferCapacity = 64;

	private readonly StringBuilder _buffer = new StringBuilder(DefaultBufferCapacity);

	private readonly Queue<Node> _challengeQueue = new Queue<Node>();

	public string GetBuffer()
	{
		return _buffer.ToString();
	}

	public void QueueChallenge(Node challenge)
	{
		if (challenge == null)
			return;

		if (MaxQueueLength > 0 && _challengeQueue.Count >= MaxQueueLength)
		{
			GD.PrintErr($"TypingManager: queue full (MaxQueueLength={MaxQueueLength}), ignoring new challenge");
			return;
		}

		_challengeQueue.Enqueue(challenge);

		if (!HasActiveChallenge())
		{
			StartNextChallenge();
		}
	}

	public void CancelChallenge()
	{
		_activeChallenge = null;
		_buffer.Clear();
	}

	private void StartNextChallenge()
	{
		if (_challengeQueue.Count == 0)
		{
			_activeChallenge = null;
			_buffer.Clear();
			return;
		}

		_activeChallenge = _challengeQueue.Dequeue();
		_buffer.Clear();
		// May want to emit a signal here to notify UI to focus input.
	}

	public void ClearQueue()
	{
		_challengeQueue.Clear();
	}

	public void AddCharacter(string ch)
	{
        if (string.IsNullOrEmpty(ch))
            return;

        int remaining = MaxBufferLength - _buffer.Length;
        if (remaining <= 0)
            return;

        string toAppend = ch;
        if (ch.Length > remaining)
        {
            toAppend = ch.Substring(0, remaining);
        }

        _buffer.Append(toAppend);
        EmitSignal(nameof(CharacterTyped), toAppend);
    }

	public void Backspace()
	{
		if (_buffer.Length == 0)
			return;

		_buffer.Length = Math.Max(0, _buffer.Length - 1);
	}

	public void ReportCompletion(string challengeId, Godot.Collections.Dictionary result)
	{
		_activeChallenge = null;
		_buffer.Clear();
		EmitSignal(nameof(WordCompleted), challengeId, result);

		StartNextChallenge();
	}

	public void ReportMistyped(Godot.Collections.Dictionary info)
	{
		EmitSignal(nameof(Mistyped), info ?? new Godot.Collections.Dictionary());
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey ev)
		{
			if (!ev.Pressed || ev.IsEcho())
				return;

			int codepoint = (int)ev.Unicode;
			if (codepoint != 0)
			{
				string ch = char.ConvertFromUtf32(codepoint);
				AddCharacter(ch);
				return;
			}

			if (ev.Keycode == Key.Backspace)
			{
				Backspace();
			}
		}
	}

	public bool HasActiveChallenge()
	{
		return _activeChallenge != null;
	}
}

