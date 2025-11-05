using Godot;
using System;
using System.Collections.Generic;

public partial class GameState : Node
{
    [Signal] public delegate void SeedsChangedEventHandler(int newTotal);

    private const int CURRENT_SAVE_VERSION = 1;
    private const string SAVE_PATH = "user://save.json";

    private IFileRepository _fileRepo;
    private SaveManager _saveManager;

    public int Seeds { get; private set; } = 0;
    public List<string> UnlockedRelics { get; private set; } = [];
    public Dictionary<string, object> Conservatory { get; private set; } = [];

    public override void _Ready()
    {
        _fileRepo = new FileRepository();
        _saveManager = new SaveManager(SAVE_PATH, _fileRepo, CURRENT_SAVE_VERSION);

        Load();
    }

    /// <summary>
    /// Adds the given amount of seeds to the player's total.
    /// </summary>
    public void AddSeeds(int amount)
    {
        if (amount <= 0)
            return;
        Seeds += amount;
        EmitSignal(nameof(SeedsChanged), Seeds);
    }

    /// <summary>
    /// Attempts to spend the given amount of seeds.
    /// </summary>
    public bool SpendSeeds(int amount)
    {
        if (amount <= 0)
            return false;
        if (Seeds < amount)
            return false;
        Seeds -= amount;
        EmitSignal(nameof(SeedsChanged), Seeds);
        return true;
    }

    /// <summary>
    /// Saves the current game state.
    /// </summary>
    public void Save()
    {
        var payload = new SavePayload
        {
            Seeds = Seeds,
            UnlockedRelics = UnlockedRelics,
            Conservatory = Conservatory
        };

        _saveManager.Save(payload);
    }

    /// <summary>
    /// Loads the game state from the save file.
    /// </summary>
    public void Load()
    {
        var payload = _saveManager.LoadOrDefault();

        Seeds = payload?.Seeds ?? 0;

        UnlockedRelics.Clear();
        if (payload?.UnlockedRelics != null)
        {
            foreach (var unlockedRelic in payload.UnlockedRelics)
                UnlockedRelics.Add(unlockedRelic);
        }
        Conservatory = payload?.Conservatory ?? [];

        EmitSignal(nameof(SeedsChanged), Seeds);
    }
}
