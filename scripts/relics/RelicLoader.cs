using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public static class RelicLoader
{
    static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true 
    };

    public static List<RelicDefinition> LoadCatalog(string path)
    {
        try
        {
            var full = path;
            if (!File.Exists(full))
            {
                GD.PrintErr($"RelicLoader: file not found: {full}");
                return [];
            }

            var txt = File.ReadAllText(full);
            var defs = JsonSerializer.Deserialize<List<RelicDefinition>>(txt, options);
            if (defs == null)
                return [];
            return defs;
        }
        catch (Exception ex)
        {
            GD.PrintErr("RelicLoader: failed to load catalog: ", ex.Message);
            return [];
        }
    }
}
