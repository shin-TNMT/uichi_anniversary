using Godot;
using System;

public partial class PromptManager : Node
{
    private CanvasLayer promptLayer = null;
    private Label promptLabel = null;

    public override void _Ready()
    {
        // Try to instantiate the scene so designers can edit it
        try
        {
            var packed = GD.Load<PackedScene>("res://scenes/ui/NPCPrompt.tscn");
            if (packed != null)
            {
                var inst = packed.Instantiate();
                if (inst is CanvasLayer cl)
                {
                    promptLayer = cl;
                    promptLabel = promptLayer.GetNodeOrNull<Label>("NPC_PromptPanel/NPC_TalkPrompt");
                    AddChild(promptLayer);
                    try { promptLayer.Visible = false; } catch { }
                    return;
                }
                else
                {
                    // If root is not CanvasLayer, search for one
                    foreach (Node c in inst.GetChildren())
                    {
                        if (c is CanvasLayer cc)
                        {
                            promptLayer = cc;
                            try { promptLabel = promptLayer.GetNodeOrNull<Label>("NPC_PromptPanel/NPC_TalkPrompt"); } catch { }
                            AddChild(promptLayer);
                            try { promptLayer.Visible = false; } catch { }
                            return;
                        }
                    }
                    inst.QueueFree();
                }
            }
        }
        catch { }

        // Fallback: create minimal prompt programmatically
        try
        {
            promptLayer = new CanvasLayer();
            promptLayer.Name = "NPCPromptLayer";
            try { promptLayer.Set("layer", 900); } catch { }

            var panel = new Panel();
            panel.Name = "NPC_PromptPanel";
            try
            {
                panel.CustomMinimumSize = new Vector2(320, 56);
                panel.AnchorLeft = 0.35f;
                panel.AnchorRight = 0.65f;
                panel.AnchorTop = 0.88f;
                panel.AnchorBottom = 0.96f;
            }
            catch { }

            try
            {
                var sb = new StyleBoxFlat();
                sb.BgColor = new Color(0, 0, 0, 0.65f);
                sb.CornerRadiusTopLeft = 8;
                sb.CornerRadiusTopRight = 8;
                sb.CornerRadiusBottomLeft = 8;
                sb.CornerRadiusBottomRight = 8;
                panel.AddThemeStyleboxOverride("panel", sb);
            }
            catch { }

            var label = new Label();
            label.Name = "NPC_TalkPrompt";
            label.Text = "話す [Enter]";
            try { label.HorizontalAlignment = HorizontalAlignment.Center; } catch { }
            try { label.AddThemeColorOverride("font_color", new Color(1, 1, 1)); } catch { }
            try { label.AddThemeFontSizeOverride("font_size", 18); } catch { }
            try { label.AnchorLeft = 0.0f; label.AnchorTop = 0.0f; label.AnchorRight = 1.0f; label.AnchorBottom = 1.0f; } catch { }
            panel.AddChild(label);
            promptLabel = label;
            promptLayer.AddChild(panel);
            AddChild(promptLayer);
            try { promptLayer.Visible = false; } catch { }
        }
        catch { }
    }

    public void ShowPrompt(string text = null)
    {
        try
        {
            if (promptLabel != null)
            {
                try { promptLabel.Text = string.IsNullOrEmpty(text) ? "話す [Enter]" : text; } catch { }
            }
            if (promptLayer != null)
            {
                try { promptLayer.Visible = true; } catch { }
            }
        }
        catch { }
    }

    public void HidePrompt()
    {
        try
        {
            if (promptLayer != null)
            {
                try { promptLayer.Visible = false; } catch { }
            }
        }
        catch { }
    }
}
