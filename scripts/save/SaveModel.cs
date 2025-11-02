using Godot;
using System;
using System.Collections.Generic;

public sealed class SavePayload
{
    public int Seeds { get; set; } = 0;
    public List<string> UnlockedRelics { get; set; } = [];
    public Dictionary<string, object> Conservatory { get; set; } = [];

    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>
        {
            ["seeds"] = Seeds
        };
        var arr = new List<string>();
        if (UnlockedRelics != null)
        {
            foreach (var s in UnlockedRelics)
                arr.Add(s);
        }
        dict["unlocked_relics"] = arr;
        dict["conservatory"] = Conservatory ?? new Dictionary<string, object>();
        return dict;
    }

    public static SavePayload FromDictionary(Dictionary<string, object> dict)
    {
        var payload = new SavePayload();
        if (dict == null) return payload;

        if (dict.ContainsKey("seeds"))
            payload.Seeds = Convert.ToInt32(dict["seeds"]);

        payload.UnlockedRelics = new List<string>();
        if (dict.ContainsKey("unlocked_relics") && dict["unlocked_relics"] is List<object> unlockedRelicsArray)
        {
            foreach (var unlockedRelic in unlockedRelicsArray)
            {
                if (unlockedRelic is string s)
                    payload.UnlockedRelics.Add(s);
            }
        }

        if (dict.ContainsKey("conservatory") && dict["conservatory"] is Dictionary<string, object> conservatoryDict)
            payload.Conservatory = conservatoryDict;

        return payload;
    }
}

public sealed class SaveRoot
{
    public int Version { get; set; } = 1;
    public SavePayload Payload { get; set; } = new SavePayload();

    public Dictionary<string, object> ToDictionary()
    {
        var rootDict = new Dictionary<string, object>
        {
            ["version"] = Version,
            ["payload"] = Payload?.ToDictionary() ?? []
        };
        return rootDict;
    }

    public static SaveRoot FromDictionary(Dictionary<string, object> dict)
    {
        if (dict == null)
            return new SaveRoot();

        var saveRoot = new SaveRoot();
        
        if (dict.ContainsKey("version"))
            saveRoot.Version = Convert.ToInt32(dict["version"]);

        if (dict.ContainsKey("payload") && dict["payload"] is Dictionary<string, object> pd)
            saveRoot.Payload = SavePayload.FromDictionary(pd);

        return saveRoot;
    }
}
