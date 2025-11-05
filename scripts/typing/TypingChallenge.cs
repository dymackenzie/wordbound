using Godot;
using System;
using System.Collections.Generic;

public partial class TypingChallenge : Control
{
	[Signal] public delegate void ProgressEventHandler(string challengeId, string typed, double remainingTime);
	[Signal] public delegate void FailedEventHandler(string challengeId, string reason);
	[Signal] public delegate void CompletedEventHandler(string challengeId, string finalBuffer, double timeLeft);

	[Export] public NodePath DisplayLabelPath { get; set; } = new NodePath();
	[Export] public double ProgressEmitIntervalMs { get; set; } = 50.0;
	[Export] public double InvalidDisplayMs { get; set; } = 300.0;
	[Export] public double LetterTimeBonusSeconds { get; set; } = 0.5;
	[Export] public double MaxRemainingSeconds { get; set; } = 12.0;
	[Export] public Color CorrectColor { get; set; } = new Color("#00ff66");
	[Export] public Color IncorrectColor { get; set; } = new Color("#ff4444");
	[Export] public Color RemainingColor { get; set; } = new Color("#888888");

	private string _challengeId = "";
	private string _text = "";
	private double _timeLimitSeconds;
	private bool _running = false;
	private ulong _deadlineTicks = 0;
	private ulong _lastProgressEmitTicks = 0;

	private int _position = 0; // how many characters correctly typed
	private string _lastInvalidChar = "";
	private ulong _lastInvalidTick = 0;

	private ulong _startTicks = 0;
	private int _correctCharCount = 0;
	private double _wpm = 0.0;

	private IWpmCalculator _wpmCalculator;
	private ITimeBonusPolicy _timeBonusPolicy;
	private TypingManager _typingManager;
	private RichTextLabel _displayLabel;

	public override void _Ready() { }

	public override void _Process(double delta)
	{
		if (!_running) return;

		ulong now = Time.GetTicksMsec();
		if (now >= _deadlineTicks)
		{
			Fail("timeout");
			return;
		}

		if (now - _lastProgressEmitTicks >= (ulong)ProgressEmitIntervalMs)
		{
			EmitProgress();
			_lastProgressEmitTicks = now;
		}
	}

	private void Start(string challengeId, string text, double timeLimitSeconds)
	{
		if (string.IsNullOrEmpty(challengeId))
			throw new ArgumentException("challengeId required", nameof(challengeId));

		_challengeId = challengeId;
		_text = text ?? "";

		_position = 0;
		_lastInvalidChar = "";

		ulong now = Time.GetTicksMsec();
		_deadlineTicks = now + (ulong)(timeLimitSeconds * 1000.0);
		_startTicks = now;
		_correctCharCount = 0;
		_wpm = 0.0;

		_wpmCalculator = new WpmCalculator();
		_timeBonusPolicy = new DefaultTimeBonusPolicy();
		_lastProgressEmitTicks = now;
		_running = true;
		Visible = true;

		ConnectToDisplayLabel();
		ConnectToManager();
		EmitProgress();
	}

	/// <summary>
    /// Prepares the typing challenge with the given parameters.
    /// </summary>
	public void Prepare(string challengeId, string text, double timeLimitSeconds)
	{
		_challengeId = challengeId ?? "";
		_text = text ?? "";
		_timeLimitSeconds = timeLimitSeconds;
		_running = false;
		_deadlineTicks = 0;
		Visible = false;
	}

	/// <summary>
	/// Starts the prepared typing challenge.
	/// </summary>
	public void ActivateQueued()
	{
		if (_running)
			return;

		if (string.IsNullOrEmpty(_challengeId))
		{
			GD.PrintErr("TypingChallenge: ActivateQueued called but no prepared challengeId was set. Call Prepare(...) first.");
			return;
		}

		Start(_challengeId, _text, _timeLimitSeconds);
	}

	/// <summary>
	/// Cancels the current typing challenge.
	/// </summary>
	public void Cancel()
	{
		Fail("cancelled");
	}

	private void OnCharacterTyped(string ch)
	{
		if (!_running)
			return;

		if (string.IsNullOrEmpty(ch))
			return;

		ulong now = Time.GetTicksMsec();

		if (_position < _text.Length && ch[0] == _text[_position])
		{
			_position++;

			// apply time bonus
			_deadlineTicks = _timeBonusPolicy.ApplyBonus(_deadlineTicks, now, LetterTimeBonusSeconds, MaxRemainingSeconds);

			_correctCharCount++;
			_wpm = _wpmCalculator.CalculateWpm(_correctCharCount, _startTicks, now);

			// notify listeners
			_typingManager?.NotifyCharacterCorrect(ch);
			EmitSignal(nameof(Progress), _challengeId, _text[.._position], RemainingTimeSeconds());
			UpdateDisplay();

			if (_position >= _text.Length)
			{
				Complete(_text);
			}
		}
		else
		{
			// invalid char: flash it and notify TypingManager and listeners
			_lastInvalidChar = ch;
			_lastInvalidTick = Time.GetTicksMsec();
			_typingManager?.ReportMistyped(new Dictionary<string, object>
			{
				["challenge_id"] = _challengeId,
				["typed"] = ch,
				["expected"] = _position < _text.Length ? _text[_position].ToString() : ""
			});
			UpdateDisplay();
		}
	}

	private void Complete(string finalBuffer)
	{
		if (!_running)
			return;

		double accuracy = AccuracyCalculator.ComputeAccuracy(_text, _typingManager.GetBuffer() ?? "");
		double timeLeft = RemainingTimeSeconds();

		// Emit Completed for UI or visual listeners
		EmitSignal(nameof(Completed), _challengeId, finalBuffer, timeLeft);


		_typingManager?.ReportCompletion(_challengeId, new Dictionary<string, object>
		{
			["typed"] = finalBuffer,
			["expected"] = _text,
			["accuracy"] = accuracy,
			["wpm"] = _wpm,
			["time_left"] = timeLeft,
			["has_half_time_left"] = timeLeft >= (MaxRemainingSeconds * 0.5)
		});

		DisconnectFromManager();
		_running = false;
		Visible = false;
	}

	private void Fail(string reason)
	{
		if (!_running)
			return;

		EmitSignal(nameof(Failed), _challengeId, reason);
		_typingManager?.ReportIncompletion(_challengeId);
		DisconnectFromManager();
		_running = false;
	}

	private void EmitProgress()
	{
		if (!_running)
			return;

		string prefix = _text[..Math.Min(_position, _text.Length)];
		EmitSignal(nameof(Progress), _challengeId, prefix, RemainingTimeSeconds());
		UpdateDisplay();
	}

	private double RemainingTimeSeconds()
	{
		if (!_running)
			return 0.0;

		ulong now = Time.GetTicksMsec();
		if (now >= _deadlineTicks)
			return 0.0;

		return (_deadlineTicks - now) / 1000.0;
	}

	private void ConnectToDisplayLabel()
	{
		if (!string.IsNullOrEmpty(DisplayLabelPath.ToString()))
		{
			_displayLabel = GetNodeOrNull<RichTextLabel>(DisplayLabelPath);
			if (_displayLabel != null)
				_displayLabel.BbcodeEnabled = true;
		}
		else
		{
			throw new ArgumentException("DisplayLabelPath is not set", nameof(DisplayLabelPath));
		}
	}

	private void ConnectToManager()
	{
		_typingManager = GetNodeOrNull<TypingManager>("/root/TypingManager");
		if (_typingManager != null)
		{
			try
			{
				_typingManager.Connect("CharacterTyped", new Callable(this, nameof(OnCharacterTyped)));
			}
			catch
			{
				GD.PrintErr("Error connecting to TypingManager CharacterTyped signal");
			}
		}
	}

	private void DisconnectFromManager()
	{
		if (_typingManager == null) return;
		try
		{
			_typingManager.Disconnect("CharacterTyped", new Callable(this, nameof(OnCharacterTyped)));
		}
		catch
		{
			GD.PrintErr("Error disconnecting from TypingManager CharacterTyped signal");
		}
	}

	private void UpdateDisplay()
	{
		if (_displayLabel == null)
			return;

		try
		{
			_displayLabel.Clear();

			// correct prefix
			if (_position > 0)
			{
				_displayLabel.PushColor(CorrectColor);
				_displayLabel.AddText(_text[.._position]);
				_displayLabel.Pop();
			}

			// invalid flash
			ulong now = Time.GetTicksMsec();
			bool showInvalid = !string.IsNullOrEmpty(_lastInvalidChar) && (now - _lastInvalidTick) <= (ulong)InvalidDisplayMs;
			if (showInvalid)
			{
				_displayLabel.PushColor(IncorrectColor);
				_displayLabel.AddText(_lastInvalidChar);
				_displayLabel.Pop();
			}

			// remaining text
			int remStart = _position + (showInvalid ? 1 : 0);
			if (remStart < _text.Length)
			{
				_displayLabel.PushColor(RemainingColor);
				_displayLabel.AddText(_text[remStart..]);
				_displayLabel.Pop();
			}
		}
		catch
		{
			_displayLabel.Text = _text;
		}
	}

}

