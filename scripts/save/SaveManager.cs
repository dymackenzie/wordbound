using Godot;
using System;

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
            var parse = JSON.ParseString(json);
            if (parse.Error != Error.Ok)
            {
                GD.PrintErr($"SaveManager.LoadOrDefault: JSON parse error: {parse.ErrorString}");
                return new SavePayload();
            }

            var rootDict = parse.Result as Godot.Collections.Dictionary;
            if (rootDict == null)
            {
                GD.PrintErr("SaveManager.LoadOrDefault: save root is not a Dictionary");
                return new SavePayload();
            }

            int version = rootDict.Contains("version") ? Convert.ToInt32(rootDict["version"]) : 1;

            var saveRoot = SaveRoot.FromDictionary(rootDict);
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
        var json = JSON.Print(root.ToDictionary(), pretty: true);
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
