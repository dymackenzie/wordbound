using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Strongly-typed save model classes and dictionary conversion helpers.
/// These provide type-safety while remaining compatible with Godot's JSON/Dictionary APIs.
/// </summary>
public sealed class SavePayload
{
    public int Seeds { get; set; } = 0;
    public List<string> UnlockedRelics { get; set; } = new List<string>();
    public Godot.Collections.Dictionary Conservatory { get; set; } = new Godot.Collections.Dictionary();

    public Godot.Collections.Dictionary ToDictionary()
    {
        var dict = new Godot.Collections.Dictionary();
        dict["seeds"] = Seeds;
        dict["unlocked_relics"] = new Godot.Collections.Array(UnlockedRelics.ToArray());
        dict["conservatory"] = Conservatory ?? new Godot.Collections.Dictionary();
        return dict;
    }

    public static SavePayload FromDictionary(Godot.Collections.Dictionary dict)
    {
        var payload = new SavePayload();
        if (dict == null) return payload;

        if (dict.Contains("seeds"))
            payload.Seeds = Convert.ToInt32(dict["seeds"]);

        payload.UnlockedRelics = new List<string>();
        if (dict.Contains("unlocked_relics"))
        {
            var unlockedRelicsArray = dict["unlocked_relics"] as Godot.Collections.Array;
            if (unlockedRelicsArray != null)
            {
                foreach (var unlockedRelic in unlockedRelicsArray)
                {
                    if (unlockedRelic is string str)
                        payload.UnlockedRelics.Add(str);
                }
            }
        }

        payload.Conservatory = dict.Contains("conservatory") ? (dict["conservatory"] as Godot.Collections.Dictionary) ?? new Godot.Collections.Dictionary() : new Godot.Collections.Dictionary();
        return payload;
    }
}

public sealed class SaveRoot
{
    public int Version { get; set; } = 1;
    public SavePayload Payload { get; set; } = new SavePayload();

    public Godot.Collections.Dictionary ToDictionary()
    {
        var rootDict = new Godot.Collections.Dictionary();
        rootDict["version"] = Version;
        rootDict["payload"] = Payload?.ToDictionary() ?? new Godot.Collections.Dictionary();
        return rootDict;
    }

    public static SaveRoot FromDictionary(Godot.Collections.Dictionary dict)
    {
        if (dict == null)
            return new SaveRoot();

        var saveRoot = new SaveRoot();
        
        if (dict.Contains("version"))
            saveRoot.Version = Convert.ToInt32(dict["version"]);

        Godot.Collections.Dictionary payloadDict = null;
        if (dict.Contains("payload"))
            payloadDict = dict["payload"] as Godot.Collections.Dictionary;
        else
            payloadDict = dict;

        saveRoot.Payload = SavePayload.FromDictionary(payloadDict);
        return saveRoot;
    }
}
