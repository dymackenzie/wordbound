using System.Collections.Generic;

public class RelicInstance(RelicDefinition def, List<IRelicEffect> effects)
{
    public RelicDefinition Definition { get; } = def;
    public int Stacks { get; set; } = 1;
    public List<IRelicEffect> Effects { get; set; } = effects;
}
