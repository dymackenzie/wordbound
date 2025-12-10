using System.Collections.Generic;
using System.Text.Json.Serialization;

public class RelicEffectDef
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("timing")] public string Timing { get; set; }
    [JsonPropertyName("params")] public System.Text.Json.JsonElement Params { get; set; }
}

public class RelicDefinition
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("name_key")] public string NameKey { get; set; }
    [JsonPropertyName("desc_key")] public string DescKey { get; set; }
    [JsonPropertyName("icon")] public string Icon { get; set; }
    [JsonPropertyName("rarity")] public string Rarity { get; set; }
    [JsonPropertyName("weight")] public float Weight { get; set; } = 1.0f;
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("stackable")] public bool Stackable { get; set; } = false;
    [JsonPropertyName("max_stacks")] public int MaxStacks { get; set; } = 1;
    [JsonPropertyName("effects")] public List<RelicEffectDef> Effects { get; set; } = [];
}
