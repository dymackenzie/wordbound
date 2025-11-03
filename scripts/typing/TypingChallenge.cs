using Godot;
using System;
using System.Collections.Generic;

public partial class TypingChallenge : Control
{
	[Signal] public delegate void ProgressEventHandler(string challengeId, string typed, double remainingTime);
	[Signal] public delegate void FailedEventHandler(string challengeId, string reason);
	[Signal] public delegate void CharacterCorrectEventHandler(string ch);
	[Signal] public delegate void CharacterInvalidEventHandler(string ch);
	[Signal] public delegate void WpmUpdatedEventHandler(double wpm);

	[Export] public NodePath DisplayLabelPath { get; set; } = new NodePath();
	[Export] public double ProgressEmitIntervalMs { get; set; } = 100.0;
	[Export] public double InvalidDisplayMs { get; set; } = 300.0;
	[Export] public double LetterTimeBonusSeconds { get; set; } = 0.5;
	[Export] public double MaxRemainingSeconds { get; set; } = 12.0;
	[Export] public string CorrectColor { get; set; } = "#00ff66";
	[Export] public string IncorrectColor { get; set; } = "#ff4444";
	[Export] public string RemainingColor { get; set; } = "#888888";

	private string _challengeId = "";
	private string _text = "";
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

	public override void _Ready() {}

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

	/// <summary>
	/// NOTE: must be called before challenge is queued with TypingManager.QueueChallenge.
	/// </summary>
	public void Start(string challengeId, string text, double timeLimitSeconds)
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

		ConnectToDisplayLabel();
		ConnectToManager();
		EmitProgress();
	}

    public void Cancel()
    {
        if (!_running)
            return;

        DisconnectFromManager();
        _running = false;
        EmitSignal(nameof(Failed), _challengeId, "cancelled");
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
			EmitSignal(nameof(WpmUpdated), _wpm);
			EmitSignal(nameof(CharacterCorrect), ch);
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
            try
            {
                _typingManager?.ReportMistyped(new Dictionary<string, object> {
                    ["challenge_id"] = _challengeId,
                    ["typed"] = ch,
                    ["expected"] = _position < _text.Length ? _text[_position].ToString() : ""
                });
            }
            catch
            {
                GD.PrintErr("Error reporting mistyped character");
            }
            UpdateDisplay();
        }
    }

	private void Complete(string finalBuffer)
	{
        if (!_running)
            return;
            
		double accuracy = 1.0;
        double timeLeft = RemainingTimeSeconds();
        bool hasHalfTimeLeft = timeLeft >= (MaxRemainingSeconds * 0.5);
        
        try
        {
            _typingManager?.ReportCompletion(_challengeId, new Dictionary<string, object> {
                ["typed"] = finalBuffer,
                ["accuracy"] = accuracy,
                ["wpm"] = _wpm,
                ["time_left"] = timeLeft,
                ["has_half_time_left"] = hasHalfTimeLeft
            });
        }
        catch
        {
            GD.PrintErr("Error reporting completion");
        }

		DisconnectFromManager();
		_running = false;
	}

	private void Fail(string reason)
	{
        if (!_running)
            return;
            
		EmitSignal(nameof(Failed), _challengeId, reason);
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
            GD.PrintErr("DisplayLabelPath is null or empty");
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
				_displayLabel.PushColor(Utility.ParseColor(CorrectColor));
				_displayLabel.AddText(_text[.._position]);
				_displayLabel.Pop();
			}

			// invalid flash
			ulong now = Time.GetTicksMsec();
			bool showInvalid = !string.IsNullOrEmpty(_lastInvalidChar) && (now - _lastInvalidTick) <= (ulong)InvalidDisplayMs;
			if (showInvalid)
			{
				_displayLabel.PushColor(Utility.ParseColor(IncorrectColor));
				_displayLabel.AddText(_lastInvalidChar);
				_displayLabel.Pop();
			}

			// remaining text
			int remStart = _position + (showInvalid ? 1 : 0);
			if (remStart < _text.Length)
			{
				_displayLabel.PushColor(Utility.ParseColor(RemainingColor));
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

