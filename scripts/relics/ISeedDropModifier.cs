using Godot;

public interface ISeedDropModifier
{
    /// <summary>
    /// Get extra number of seed drops to spawn upon enemy death.
    /// </summary>
    int GetExtraDrops(Node owner, RelicInstance instance, Node enemy);
}
