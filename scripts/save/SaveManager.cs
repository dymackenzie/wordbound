using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public sealed class SaveManager
{
    private readonly string _savePath;
    private readonly FileRepository _repo;
    private readonly int _currentVersion;

    public SaveManager(string savePath, FileRepository repo, int currentVersion)
    {
        _savePath = savePath;
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _currentVersion = currentVersion;
    }

    public SavePayload LoadOrDefault()
    {
        if (!_repo.Exists(_savePath))
            return new SavePayload();

        try
        {
            var json = _repo.Load(_savePath);
            var parse = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (parse == null)
            {
                GD.PrintErr($"SaveManager.LoadOrDefault: JSON parse error");
                return new SavePayload();
            }

            int version = parse.ContainsKey("version") ? Convert.ToInt32(parse["version"]) : 1;

            var saveRoot = SaveRoot.FromDictionary(parse);
            return saveRoot.Payload ?? new SavePayload();
        }
        catch (Exception e)
        {
            GD.PrintErr($"SaveManager.LoadOrDefault failed: {e.Message}");
            return new SavePayload();
        }
    }

    public void Save(SavePayload payload)
    {
        var root = new SaveRoot {
            Version = _currentVersion,
            Payload = payload ?? new SavePayload()
        };
        var json = JsonSerializer.Serialize(root.ToDictionary(), new JsonSerializerOptions { WriteIndented = true });
        try
        {
            _repo.Save(_savePath, json);
        }
        catch (Exception e)
        {
            GD.PrintErr($"SaveManager.Save failed: {e.Message}");
        }
    }
}
