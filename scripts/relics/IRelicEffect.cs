using Godot;

public interface IRelicEffect
{
    // called when the relic is equipped; instance may hold state.
    void OnEquip(Node owner, RelicInstance instance);
    void OnUnequip(Node owner, RelicInstance instance);

    // optional hooks; default implementations may be empty when effect doesn't care.
    void OnWordComplete(Node owner, RelicInstance instance, string word) { }
    void OnKill(Node owner, RelicInstance instance) { }
    void OnDash(Node owner, RelicInstance instance) { }
}
