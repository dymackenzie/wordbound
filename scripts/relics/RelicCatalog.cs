using System.Collections.Generic;
using Godot;

public static class RelicCatalog
{
    private static readonly Dictionary<string, RelicDefinition> _byId = [];

    public static void LoadFromFile(string path)
    {
        var defs = RelicLoader.LoadCatalog(path);
        _byId.Clear();
        foreach (var d in defs)
        {
            if (string.IsNullOrEmpty(d.Id)) continue;
            _byId[d.Id] = d;
        }
        GD.Print($"RelicCatalog: loaded {_byId.Count} relics from {path}");
    }

    public static RelicDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_byId.TryGetValue(id, out var d)) return d;
        return null;
    }

    public static IEnumerable<RelicDefinition> All() => _byId.Values;
}
