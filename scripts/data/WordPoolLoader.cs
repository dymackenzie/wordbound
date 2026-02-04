using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class WordPoolLoader
{
    private static readonly Dictionary<string, Dictionary<int, List<string>>> _cache = [];

    public static Dictionary<int, List<string>> LoadPool(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_cache.ContainsKey(path)) return _cache[path];

        var mapping = new Dictionary<int, List<string>>();
        try
        {
            using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var txt = f.GetAsText();
            using var doc = JsonDocument.Parse(txt);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) 
            {
                return mapping;
            }
            
            foreach (var prop in root.EnumerateObject())
            {
                if (int.TryParse(prop.Name, out int key) && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var elem in prop.Value.EnumerateArray())
                        if (elem.ValueKind == JsonValueKind.String)
                            list.Add(elem.GetString());
                    mapping[key] = list;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WordPoolLoader: failed to load pool '{path}': {ex.Message}");
        }

        _cache[path] = mapping;
        return mapping;
    }
}
