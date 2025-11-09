using Godot;
using System;
using System.Collections.Generic;

public partial class DashGhost : Node2D
{
    [Export] public float Lifetime { get; set; } = 0.4f;
    [Export] public float InitialOpacity { get; set; } = 0.6f;

    /// <summary>
    /// Collects all CanvasItem descendants, sets their initial opacity and starts a single
    /// Tween that fades them to transparent over Lifetime seconds.
    /// </summary>
    public override async void _Ready()
    {
        var canvasItems = new List<CanvasItem>();
        CollectCanvasItems(this, canvasItems);

        // apply initial opacity to every collected CanvasItem.
        foreach (var item in canvasItems)
        {
            var col = item.Modulate;
            item.Modulate = new Color(col.R, col.G, col.B, InitialOpacity);
        }

        // create one tween and schedule a fade to 0 alpha for each item.
        var tween = CreateTween();
        foreach (var item in canvasItems)
        {
            var start = item.Modulate;
            var target = new Color(start.R, start.G, start.B, 0f);
            tween.TweenProperty(item, "modulate", target, Lifetime);
        }

        await ToSignal(GetTree().CreateTimer(Lifetime), "timeout");
        QueueFree();
    }

    private void CollectCanvasItems(Node node, List<CanvasItem> outList)
    {
        if (node is CanvasItem ci)
            outList.Add(ci);

        foreach (Node child in node.GetChildren())
        {
            if (child != null)
                CollectCanvasItems(child, outList);
        }
    }
}
