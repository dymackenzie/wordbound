using Godot;
using System;
using System.Collections.Generic;

public partial class GameState : Node
{
    [Signal] public delegate void seeds_changed(int new_total);

    private const int CURRENT_SAVE_VERSION = 1;
    private const string SAVE_PATH = "user://save.json";

    private FileRepository _fileRepo;
    private SaveManager _saveManager;

    [Export] public int Seeds { get; private set; } = 0;
    [Export] public List<string> UnlockedRelics { get; private set; } = [];
    [Export] public Godot.Collections.Dictionary Conservatory { get; private set; } = new Godot.Collections.Dictionary();

    public override void _Ready()
    {
        _fileRepo = new FileRepository();
        _saveManager = new SaveManager(SAVE_PATH, _fileRepo, CURRENT_SAVE_VERSION);

        Load();
    }

    public void AddSeeds(int amount)
    {
        if (amount <= 0)
            return;
        Seeds += amount;
        EmitSignal("seeds_changed", Seeds);
    }

    public bool SpendSeeds(int amount)
    {
        if (amount <= 0)
            return false;
        if (Seeds < amount)
            return false;
        Seeds -= amount;
        EmitSignal("seeds_changed", Seeds);
        return true;
    }

    public void Save()
    {
        var payload = new SavePayload
        {
            Seeds = Seeds,
            UnlockedRelics = new List<string>(UnlockedRelics),
            Conservatory = Conservatory
        };

        _saveManager.Save(payload);
    }

    public void Load()
    {
        var payload = _saveManager.LoadOrDefault();

        Seeds = payload?.Seeds ?? 0;
        UnlockedRelics = payload?.UnlockedRelics ?? new List<string>();
        Conservatory = payload?.Conservatory ?? new Godot.Collections.Dictionary();

        EmitSignal("seeds_changed", Seeds);
    }
}
