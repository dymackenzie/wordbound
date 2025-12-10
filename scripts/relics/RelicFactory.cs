using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

public static class RelicFactory
{
    // relationship of effect type strings to factory functions
        private static readonly Dictionary<string, Func<RelicEffectDef, IRelicEffect>> _factories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "StatModifier", def => new BasicStatModifier(def) },
            { "SeedBonus", def => new SeedBonus(def) },

            // note: add new effect types here
        };

    public static IRelicEffect CreateEffect(RelicEffectDef def)
    {
        if (def == null || string.IsNullOrEmpty(def.Type))
            return null;

        if (_factories.TryGetValue(def.Type, out var factory))
        {
            try 
            { 
                return factory(def); 
            }
            catch (Exception ex) 
            { 
                GD.PrintErr($"RelicFactory: effect factory {def.Type} threw: {ex.Message}"); 
                return null;
            }
        }

        GD.PrintErr($"RelicFactory: unknown effect type '{def.Type}'");
        return null;
    }
}

public class SeedBonus : IRelicEffect, ISeedDropModifier
{
    private readonly int _extra = 1;

    public SeedBonus(RelicEffectDef def)
    {
        if (def.Params.TryGetProperty("extra", out var v)) _extra = v.GetInt32();
    }

    public void OnEquip(Node owner, RelicInstance instance) { }
    public void OnUnequip(Node owner, RelicInstance instance) { }

    public int GetExtraDrops(Node owner, RelicInstance instance, Node enemy)
    {
        int stacks = Math.Max(1, instance?.Stacks ?? 1);
        return _extra * stacks;
    }

    public void OnKill(Node owner, RelicInstance instance) { }
}

public class BasicStatModifier : IRelicEffect
{
    private readonly string _stat = "";
    private readonly string _mode = "add";
    private readonly float _value = 0f;

    private object _originalValue = null;
    private bool _applied = false;

    public BasicStatModifier(RelicEffectDef def)
    {
        if (def.Params.TryGetProperty("stat", out var s)) _stat = s.GetString();
        if (def.Params.TryGetProperty("mode", out var m)) _mode = m.GetString();
        if (def.Params.TryGetProperty("value", out var v)) _value = v.GetSingle();
    }

    public void OnEquip(Node owner, RelicInstance instance)
    {
        if (string.IsNullOrEmpty(_stat)) return;

        var t = owner.GetType();
        var property = t.GetProperty(_stat);
        if (property == null)
        {
            GD.PrintErr($"BasicStatModifier: owner has no property '{_stat}'");
            return;
        }

        var currentObject = property.GetValue(owner);
        if (currentObject == null)
        {
            GD.PrintErr($"BasicStatModifier: property '{_stat}' is null on owner");
            return;
        }

        _originalValue = currentObject;

        double current = Convert.ToDouble(currentObject);
        if (_mode.Equals("mul", StringComparison.OrdinalIgnoreCase))
        {
            current *= Math.Pow(_value, instance.Stacks);
        }
        else
        {
            current += _value * instance.Stacks;
        }

        try
        {
            property.SetValue(owner, Convert.ChangeType(current, property.PropertyType));
            _applied = true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"BasicStatModifier: failed to set property '{_stat}': {ex.Message}");
        }
    }

    public void OnUnequip(Node owner, RelicInstance instance)
    {
        if (!_applied) return;
        if (_originalValue == null) return;

        var t = owner.GetType();
        var property = t.GetProperty(_stat);
        if (property == null) return;

        try
        {
            property.SetValue(owner, _originalValue);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"BasicStatModifier: failed to revert property '{_stat}': {ex.Message}");
        }
        finally
        {
            _applied = false;
            _originalValue = null;
        }
    }
}
