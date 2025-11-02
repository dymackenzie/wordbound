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
                gdDict[kvp.Key] = (Variant) kvp.Value;
            }
        }
        return gdDict;
    }
}