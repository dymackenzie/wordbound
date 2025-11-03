using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public interface ISaveManager
{
    /// <summary>
    /// Load the save payload, or return a default payload if no save exists.
    /// </summary>
    SavePayload LoadOrDefault();

    /// <summary>
    /// Save the given payload.
    /// Parameters:
    ///   payload - The save payload to save.
    /// </summary>
    void Save(SavePayload payload);
}

public sealed class SaveManager : ISaveManager
{
    private readonly string _savePath;
    private readonly IFileRepository _repo;
    private readonly int _currentVersion;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public SaveManager(string savePath, IFileRepository repo, int currentVersion)
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
        var root = new SaveRoot
        {
            Version = _currentVersion,
            Payload = payload ?? new SavePayload()
        };
        var json = JsonSerializer.Serialize(root.ToDictionary(), _jsonOptions);
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
