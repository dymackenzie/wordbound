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

    public static string ReverseString(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        var arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}