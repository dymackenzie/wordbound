using System.Collections.Generic;
using Godot;

public partial class RelicManager : Node
{
    private Dictionary<string, RelicInstance> _equipped = [];

    public override void _Ready()
    {
        // load relic catalog to ensure it's initialized
        if (RelicCatalog.All() == null)
        {
            RelicCatalog.LoadFromFile("res://data/relics/relics.json");
        }
    }

    public bool EquipRelic(string relicId)
    {
        if (string.IsNullOrEmpty(relicId)) return false;
        if (_equipped.ContainsKey(relicId)) return false;

        var def = RelicCatalog.Get(relicId);
        if (def == null) 
        { 
            GD.PrintErr($"RelicManager: unknown relic id {relicId}"); 
            return false; 
        }

        // build effect instances for all effect defs
        var effects = new List<IRelicEffect>();
        if (def.Effects != null)
        {
            foreach (var effDef in def.Effects)
            {
                var created = RelicFactory.CreateEffect(effDef);
                effects.Add(created);
            }
        }

        if (effects.Count == 0) 
        {
            effects.Add(new NullRelicEffect());
        }

        var inst = new RelicInstance(def, effects);
        _equipped[relicId] = inst;

        // call OnEquip on each effect
        foreach (var e in inst.Effects)
            e.OnEquip(GetParent(), inst);

        return true;
    }

    public bool UnequipRelic(string relicId)
    {
        if (!_equipped.TryGetValue(relicId, out var inst))
        {
            return false;
        }

        // call OnUnequip on each effect
        foreach (var e in inst.Effects)
            e.OnUnequip(GetParent(), inst);
        _equipped.Remove(relicId);
        
        return true;
    }

    public IEnumerable<RelicInstance> GetEquipped() => _equipped.Values;

    // event notifications
    public void NotifyWordComplete(string word)
    {
        foreach (var inst in _equipped.Values)
            foreach (var e in inst.Effects)
                e.OnWordComplete(GetParent(), inst, word);
    }

    public void NotifyKill()
    {
        foreach (var inst in _equipped.Values)
            foreach (var e in inst.Effects)
                e.OnKill(GetParent(), inst);
    }

    public void NotifyDash()
    {
        foreach (var inst in _equipped.Values)
            foreach (var e in inst.Effects)
                e.OnDash(GetParent(), inst);
    }
}

public class NullRelicEffect : IRelicEffect
{
    public void OnEquip(Node owner, RelicInstance instance) { }
    public void OnUnequip(Node owner, RelicInstance instance) { }
}
