using Godot;
using System;
using System.Text;
using System.Collections.Generic;

public partial class TypingManager : Node
{
	[Signal] public delegate void CharacterTypedEventHandler(string ch);
	[Signal] public delegate void CharacterCorrectEventHandler(string ch);
	[Signal] public delegate void MistypedEventHandler(Godot.Collections.Dictionary info);
	[Signal] public delegate void WordCompletedEventHandler(string challengeId, Godot.Collections.Dictionary result);
	[Signal] public delegate void WordIncompleteEventHandler(string challengeId);

	[Export] public int MaxBufferLength { get; set; } = 256;

	private const int DefaultBufferCapacity = 64;
	private readonly StringBuilder _buffer = new(DefaultBufferCapacity);

	public string GetBuffer()
	{
		return _buffer.ToString();
	}

	public string GetLastTypedCharacter()
	{
		if (_buffer.Length == 0)
			return "";

		return _buffer[^1].ToString();
	}

	public void ClearBuffer()
	{
		_buffer.Clear();
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
            toAppend = ch[..remaining];
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

	public void ReportCompletion(string challengeId, Dictionary<string, object> result)
	{
		if (result == null)
			result = new Dictionary<string, object>();

		_buffer.Clear();
		EmitSignal(nameof(WordCompleted), challengeId, Utility.ConvertDictionaryToGodotDictionary(result));
	}

	public void ReportMistyped(Dictionary<string, object> info)
	{
		EmitSignal(nameof(Mistyped), Utility.ConvertDictionaryToGodotDictionary(info ?? []));
	}

	public void ReportIncompletion(string challengeId)
	{
		_buffer.Clear();
		EmitSignal(nameof(WordIncomplete), challengeId);
	}

	public void NotifyCharacterCorrect(string ch)
	{
		EmitSignal(nameof(CharacterCorrect), ch);
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
}

