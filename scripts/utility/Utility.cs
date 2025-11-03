using Godot;
using System;

public static class Utility
{
    public static Godot.Collections.Dictionary ConvertDictionaryToGodotDictionary(System.Collections.Generic.Dictionary<string, object> dict)
    {
        var gdDict = new Godot.Collections.Dictionary();
        if (dict != null)
        {
            foreach (var kvp in dict)
            {
                gdDict[kvp.Key] = (Variant)kvp.Value;
            }
        }
        return gdDict;
    }

	public static Color ParseColor(string hex)
	{
		if (string.IsNullOrEmpty(hex)) return Colors.White;
		string s = hex.Trim();
		if (s.StartsWith('#')) s = s[1..];
		uint r = 255, g = 255, b = 255, a = 255;
		try
		{
			if (s.Length == 6)
			{
				r = Convert.ToUInt32(s.Substring(0, 2), 16);
				g = Convert.ToUInt32(s.Substring(2, 2), 16);
				b = Convert.ToUInt32(s.Substring(4, 2), 16);
			}
			else if (s.Length == 8)
			{
				r = Convert.ToUInt32(s.Substring(0, 2), 16);
				g = Convert.ToUInt32(s.Substring(2, 2), 16);
				b = Convert.ToUInt32(s.Substring(4, 2), 16);
				a = Convert.ToUInt32(s.Substring(6, 2), 16);
			}
		}
        catch
        {
            GD.PrintErr("Error parsing color");
        }
		return new Color(
            r / 255.0f,
            g / 255.0f,
            b / 255.0f,
            a / 255.0f
        );
	}
}