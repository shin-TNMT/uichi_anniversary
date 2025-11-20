using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class DialogManager : Node
{
    private Control dialogBoxInstance;
    private PackedScene dialogBoxScene;
    private PackedScene dialogWindowScene;
    private bool dialogBoxSceneValid = false;
    private bool dialogWindowSceneValid = false;
    private bool pausedByDialog = false;
    // Debug: force start a default dialogue at scene load to verify UI behavior
    // Set to false for normal runs (was true during debugging and caused the UI to appear at startup)
    private static bool ForceStartForDebug = false;
    private const string ForceStartResource = "res://dialogues/npc_default.json";
    private const string ForceStartNpcId = "debug_npc";
    private Godot.Collections.Dictionary<string, bool> seen = new Godot.Collections.Dictionary<string, bool>();

    private Godot.Collections.Array nodes = null;
    private string currentNpcId = null;
    private Godot.Collections.Dictionary currentNodeMap = null; // id -> node dict
    private string currentNodeId = null;
    // Shared NPC prompt HUD
    private CanvasLayer npcPromptLayer = null;
    private Label npcPromptLabel = null;
    // track which Button instance IDs we've connected to avoid duplicate connect attempts
    // Use ulong to match Godot's GetInstanceId() return type on recent C# bindings
    private HashSet<ulong> connectedButtonIds = new HashSet<ulong>();

    public override void _Ready()
    {
        // preload DialogBox scene
        dialogBoxScene = GD.Load<PackedScene>("res://scenes/ui/DialogBox.tscn");
        // Validate the PackedScene once at startup to avoid repeated instantiate errors at runtime
        if (dialogBoxScene != null)
        {
            try
            {
                var testInst = dialogBoxScene.Instantiate();
                if (testInst != null)
                {
                    // immediate free; this is only a validation
                    if (testInst is Node n) n.QueueFree();
                    dialogBoxSceneValid = true;
                    GD.Print("DialogManager: DialogBox PackedScene validated OK");
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("DialogManager: DialogBox PackedScene validation failed - will use fallback UI. Error: ", e.Message);
                // mark invalid so we won't try to instantiate it at runtime
                dialogBoxScene = null;
                dialogBoxSceneValid = false;
            }
        }

        // preload a modal DialogWindow scene (preferred for modal display)
        dialogWindowScene = GD.Load<PackedScene>("res://scenes/ui/DialogWindow.tscn");
        if (dialogWindowScene != null)
        {
            try
            {
                var testInstW = dialogWindowScene.Instantiate();
                if (testInstW != null)
                {
                    if (testInstW is Node nn) nn.QueueFree();
                    dialogWindowSceneValid = true;
                    GD.Print("DialogManager: DialogWindow PackedScene validated OK");
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("DialogManager: DialogWindow PackedScene validation failed - will fallback to embedded dialog. Error: ", e.Message);
                dialogWindowScene = null;
                dialogWindowSceneValid = false;
            }
        }

        // Debug: optionally force-start a dialogue to verify that the UI appears
        if (ForceStartForDebug)
        {
            GD.Print("DialogManager: ForceStartForDebug is enabled, attempting StartDialogue with ", ForceStartResource);
            var ok = StartDialogue(ForceStartResource, ForceStartNpcId);
            GD.Print("DialogManager: ForceStartForDebug StartDialogue returned: ", ok);
        }
    }

    // Show a shared NPC prompt at bottom-center. Text may be null to use default.
    public void ShowNPCPrompt(string text)
    {
        try
        {
            // Prefer the global PromptManager autoload if present
            try
            {
                var pm = GetTree().Root.GetNodeOrNull("PromptManager");
                if (pm != null)
                {
                    var m = pm.GetType().GetMethod("ShowPrompt");
                    if (m != null)
                    {
                        m.Invoke(pm, new object[] { string.IsNullOrEmpty(text) ? "話す [Enter]" : text });
                        return;
                    }
                }
            }
            catch { }

            if (npcPromptLayer == null)
            {
                // fallback to existing PackedScene/programmatic creation (unchanged)
                var packed = GD.Load<PackedScene>("res://scenes/ui/NPCPrompt.tscn");
                if (packed != null)
                {
                    var inst = packed.Instantiate();
                    if (inst is CanvasLayer cl)
                    {
                        npcPromptLayer = cl;
                        try { npcPromptLabel = npcPromptLayer.GetNodeOrNull<Label>("NPC_PromptPanel/NPC_TalkPrompt"); } catch { }
                        this.AddChild(npcPromptLayer);
                    }
                    else
                    {
                        CanvasLayer found = null;
                        foreach (Node c in inst.GetChildren())
                        {
                            if (c is CanvasLayer cc)
                            {
                                found = cc;
                                break;
                            }
                        }
                        if (found != null)
                        {
                            npcPromptLayer = found;
                            try { npcPromptLabel = npcPromptLayer.GetNodeOrNull<Label>("NPC_PromptPanel/NPC_TalkPrompt"); } catch { }
                            this.AddChild(npcPromptLayer);
                        }
                        else
                        {
                            inst.QueueFree();
                        }
                    }
                }

                if (npcPromptLayer == null)
                {
                    npcPromptLayer = new CanvasLayer();
                    npcPromptLayer.Name = "NPCPromptLayer";
                    try { npcPromptLayer.Set("layer", 900); } catch { }

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
                    label.Text = string.IsNullOrEmpty(text) ? "話す [Enter]" : text;
                    try { label.HorizontalAlignment = HorizontalAlignment.Center; } catch { }
                    try { label.AddThemeColorOverride("font_color", new Color(1, 1, 1)); } catch { }
                    try { label.AddThemeFontSizeOverride("font_size", 18); } catch { }
                    try { label.AnchorLeft = 0.0f; label.AnchorTop = 0.0f; label.AnchorRight = 1.0f; label.AnchorBottom = 1.0f; } catch { }
                    panel.AddChild(label);
                    npcPromptLabel = label;
                    npcPromptLayer.AddChild(panel);
                    this.AddChild(npcPromptLayer);
                }
            }

            try { npcPromptLabel.Text = string.IsNullOrEmpty(text) ? "話す [Enter]" : text; } catch { }
            try { var p = npcPromptLabel.GetParent() as CanvasItem; if (p != null) p.Visible = true; } catch { }
            try { npcPromptLabel.Visible = true; } catch { }
        }
        catch { }
    }

    public void HideNPCPrompt()
    {
        try
        {
            // prefer autoload PromptManager if present
            try
            {
                var pm = GetTree().Root.GetNodeOrNull("PromptManager");
                if (pm != null)
                {
                    var m = pm.GetType().GetMethod("HidePrompt");
                    if (m != null) { m.Invoke(pm, null); return; }
                }
            }
            catch { }

            if (npcPromptLabel != null) npcPromptLabel.Visible = false;
            if (npcPromptLayer != null)
            {
                try { var p = npcPromptLayer.GetParent(); if (p != null) p.RemoveChild(npcPromptLayer); } catch { }
                try { npcPromptLayer.QueueFree(); } catch { }
                npcPromptLayer = null;
            }
            npcPromptLabel = null;
        }
        catch { }
    }

    // Ensure a node and its children keep processing while the SceneTree is paused
    private void SetPauseModeRecursively(Node root)
    {
        if (root == null)
            return;
        try
        {
            // Use property setter by name to avoid compile-time binding issues across Godot C# versions
            // 'pause_mode' corresponds to the GDScript property; 2 == Process
            root.Set("pause_mode", 2);
        }
        catch { }
        var children = root.GetChildren();
        foreach (Node c in children)
        {
            SetPauseModeRecursively(c);
        }
    }

    // Ensure a Button is enabled to receive mouse/focus input even when tree paused
    private void EnsureButtonInteractive(Button btn)
    {
        if (btn == null) return;
        try { btn.Disabled = false; } catch { }
        try { btn.Set("mouse_filter", 0); } catch { }
        try { btn.GrabFocus(); } catch { }
    }

    public bool StartDialogue(string resourcePath, string npcId)
    {
        if (IsActive())
            return false;
        if (string.IsNullOrEmpty(resourcePath))
            return false;

        try
        {
            var file = FileAccess.Open(resourcePath, FileAccess.ModeFlags.Read);
            var text = file.GetAsText();
            file.Close();

            // Parse JSON using System.Text.Json and map to Godot collections
            using var doc = System.Text.Json.JsonDocument.Parse(text);
            var root = doc.RootElement;

            currentNodeMap = new Godot.Collections.Dictionary();
            // nodes
            if (root.TryGetProperty("nodes", out var nodesElem) && nodesElem.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var n in nodesElem.EnumerateArray())
                {
                    var nd = new Godot.Collections.Dictionary();
                    if (n.TryGetProperty("id", out var idEl)) nd["id"] = idEl.GetString();
                    if (n.TryGetProperty("type", out var typeEl)) nd["type"] = typeEl.GetString();
                    if (n.TryGetProperty("speaker", out var spEl)) nd["speaker"] = spEl.GetString();
                    if (n.TryGetProperty("text", out var tEl)) nd["text"] = tEl.GetString();
                    if (n.TryGetProperty("next", out var nxEl)) nd["next"] = nxEl.GetString();
                    if (n.TryGetProperty("once", out var onceEl) && onceEl.ValueKind == System.Text.Json.JsonValueKind.True) nd["once"] = true;
                    if (n.TryGetProperty("choices", out var chEl) && chEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = new Godot.Collections.Array();
                        foreach (var choice in chEl.EnumerateArray())
                        {
                            var cd = new Godot.Collections.Dictionary();
                            if (choice.TryGetProperty("text", out var ctext)) cd["text"] = ctext.GetString();
                            if (choice.TryGetProperty("next", out var cnext)) cd["next"] = cnext.GetString();
                            arr.Add(cd);
                        }
                        nd["choices"] = arr;
                    }
                    if (n.TryGetProperty("actions", out var actEl) && actEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var aarr = new Godot.Collections.Array();
                        foreach (var act in actEl.EnumerateArray())
                        {
                            var ad = new Godot.Collections.Dictionary();
                            // copy all string properties from action object
                            foreach (var p in act.EnumerateObject())
                            {
                                if (p.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                    ad[p.Name] = p.Value.GetString();
                            }
                            aarr.Add(ad);
                        }
                        nd["actions"] = aarr;
                    }
                    if (nd.ContainsKey("id"))
                        currentNodeMap[(string)nd["id"]] = nd;
                }
            }

            // choose start node based on seen flag
            string startId = null;
            if (!string.IsNullOrEmpty(npcId) && seen.ContainsKey(npcId) && seen[npcId] && root.TryGetProperty("start_seen", out var ss))
                startId = ss.GetString();
            else if (root.TryGetProperty("start", out var s))
                startId = s.GetString();

            if (startId == null)
                return false;

            currentNpcId = npcId;
            currentNodeId = startId;

            GD.Print("StartDialogue: npcId=", npcId, " startId=", startId, " nodes=", currentNodeMap.Count);

            ShowDialogBox();
            ShowCurrentNode();
            return true;
        }
        catch (Exception e)
        {
            GD.PrintErr("StartDialogue failed: ", e.Message);
            return false;
        }
    }

    private void ShowDialogBox()
    {
        if (dialogBoxInstance == null)
        {
            // Prefer a modal WindowDialog if available
            if (dialogWindowScene != null && dialogWindowSceneValid)
            {
                try
                {
                    var instW = dialogWindowScene.Instantiate();
                    Control hostControl = null;
                    // Prefer the instance itself if it's a Control (WindowDialog etc.)
                    if (instW is Control c)
                    {
                        hostControl = c;
                        // Wrap the control in a CanvasLayer to ensure it is rendered and receives input above other UI
                        try
                        {
                            var layer = new CanvasLayer();
                            layer.Name = "DialogCanvasLayer";
                            try { layer.Set("layer", 1000); } catch { }
                            layer.AddChild(hostControl);
                            GetTree().Root.AddChild(layer);
                        }
                        catch
                        {
                            // fallback: add control directly
                            GetTree().Root.AddChild(hostControl);
                        }
                    }
                    else
                    {
                        // Try to find a Control child inside the instantiated scene
                        foreach (Node child in instW.GetChildren())
                        {
                            if (child is Control cc)
                            {
                                hostControl = cc;
                                break;
                            }
                        }
                        // If still not found, wrap the instance into a Panel so we have a Control root
                        if (hostControl == null)
                        {
                            var wrapper = new Panel();
                            wrapper.Name = "DialogWindow_Wrapper";
                            wrapper.AddChild((Node)instW);
                            hostControl = wrapper;
                            try
                            {
                                var layer = new CanvasLayer();
                                layer.Name = "DialogCanvasLayer";
                                try { layer.Set("layer", 1000); } catch { }
                                layer.AddChild(hostControl);
                                GetTree().Root.AddChild(layer);
                            }
                            catch
                            {
                                GetTree().Root.AddChild(wrapper);
                            }
                        }
                        else
                        {
                            // add the original instance to the root so child control is in the tree
                            try
                            {
                                var layer = new CanvasLayer();
                                layer.Name = "DialogCanvasLayer";
                                try { layer.Set("layer", 1000); } catch { }
                                layer.AddChild(instW);
                                GetTree().Root.AddChild(layer);
                            }
                            catch
                            {
                                GetTree().Root.AddChild(instW);
                            }
                        }
                    }

                    if (hostControl != null)
                    {
                        dialogBoxInstance = hostControl;
                        dialogBoxInstance.Name = "DialogWindow";
                        // Previously we paused the SceneTree here which prevented UI clicks reaching the dialog on some setups.
                        // Avoid pausing the entire tree; keep gameplay input handling responsibility to other systems.
                        pausedByDialog = false;
                        // Ensure the dialog UI still receives input while the tree is paused
                        try
                        {
                            SetPauseModeRecursively(dialogBoxInstance);
                        }
                        catch { }
                        // Try to bring dialog to front so it receives mouse events.
                        // Use dynamic call to 'raise' if available, otherwise set a high z_index.
                        try
                        {
                            if (dialogBoxInstance.HasMethod("raise"))
                                dialogBoxInstance.Call("raise");
                            else
                                dialogBoxInstance.Set("z_index", 1000);
                        }
                        catch { }
                        // If WindowDialog, popup centered (call only if method exists)
                        try { if (dialogBoxInstance.HasMethod("popup_centered")) dialogBoxInstance.Call("popup_centered"); } catch { }
                        // connect Next button if present
                        var next = dialogBoxInstance.GetNodeOrNull<Button>("Panel/VBox/Footer/NextButton");
                        if (next != null)
                        {
                            // ensure interactive
                            EnsureButtonInteractive(next);
                            // attach pressed handler if not already connected
                            try
                            {
                                var id = next.GetInstanceId();
                                if (!connectedButtonIds.Contains(id))
                                {
                                    next.Pressed += OnNextPressed;
                                    connectedButtonIds.Add(id);
                                }
                            }
                            catch
                            {
                                try { next.Pressed += OnNextPressed; } catch { }
                            }
                            // Diagnostic: log GUI input events on the Next button to see if clicks reach it
                            try
                            {
                                next.GuiInput += (InputEvent ev) =>
                                {
                                    try { GD.Print($"DEBUG: NextButton GuiInput: type={ev.GetType().Name} pressed_btn={ev.IsPressed()}"); } catch { GD.Print("DEBUG: NextButton GuiInput event"); }
                                };
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr("DialogWindow instantiate failed: ", e.Message);
                    // fallback to existing dialogBoxScene or programmatic UI below
                    dialogWindowScene = null;
                    dialogWindowSceneValid = false;
                }
            }
            // If still null, fall back to previous behavior (embedded DialogBox or programmatic)
            if (dialogBoxInstance == null)
            {
                        if (dialogBoxScene != null)
                {
                    try
                    {
                        dialogBoxInstance = (Control)dialogBoxScene.Instantiate();
                        AddChild(dialogBoxInstance);
                        dialogBoxInstance.Name = "DialogBox";
                        // connect Next button
                                var next = dialogBoxInstance.GetNodeOrNull<Button>("Panel/VBox/Footer/NextButton");
                                if (next != null)
                                {
                                    EnsureButtonInteractive(next);
                                    try
                                    {
                                        var id = next.GetInstanceId();
                                        if (!connectedButtonIds.Contains(id))
                                        {
                                            next.Pressed += OnNextPressed;
                                            connectedButtonIds.Add(id);
                                        }
                                    }
                                    catch { try { next.Pressed += OnNextPressed; } catch { } }
                                }
                    }
                    catch (Exception e)
                    {
                        GD.PrintErr("DialogBox instantiate failed: ", e.Message);
                        // fallback: create a minimal dialog UI programmatically
                        dialogBoxInstance = new Control();
                        dialogBoxInstance.Name = "DialogBox";
                        var panel = new Panel();
                        panel.Name = "Panel";
                        dialogBoxInstance.AddChild(panel);
                        var vbox = new VBoxContainer();
                        vbox.Name = "VBox";
                        panel.AddChild(vbox);
                        var header = new HBoxContainer();
                        header.Name = "Header";
                        vbox.AddChild(header);
                        var speaker = new Label();
                        speaker.Name = "Speaker";
                        speaker.Text = "";
                        header.AddChild(speaker);
                        var body = new RichTextLabel();
                        body.Name = "Body";
                        body.BbcodeEnabled = false;
                        vbox.AddChild(body);
                        var choices = new VBoxContainer();
                        choices.Name = "Choices";
                        choices.Visible = false;
                        vbox.AddChild(choices);
                        var footer = new HBoxContainer();
                        footer.Name = "Footer";
                        vbox.AddChild(footer);
                        var nextbtn = new Button();
                        nextbtn.Name = "NextButton";
                        nextbtn.Text = "次へ";
                        footer.AddChild(nextbtn);
                        EnsureButtonInteractive(nextbtn);
                        nextbtn.Pressed += OnNextPressed;
                        AddChild(dialogBoxInstance);
                    }
                }
                else
                {
                    // No scene and no instance: create a minimal dialog UI
                    dialogBoxInstance = new Control();
                    dialogBoxInstance.Name = "DialogBox";
                    var panel = new Panel();
                    panel.Name = "Panel";
                    dialogBoxInstance.AddChild(panel);
                    var vbox = new VBoxContainer();
                    vbox.Name = "VBox";
                    panel.AddChild(vbox);
                    var header = new HBoxContainer();
                    header.Name = "Header";
                    vbox.AddChild(header);
                    var speaker = new Label();
                    speaker.Name = "Speaker";
                    speaker.Text = "";
                    header.AddChild(speaker);
                    var body = new RichTextLabel();
                    body.Name = "Body";
                    body.BbcodeEnabled = false;
                    vbox.AddChild(body);
                    var choices = new VBoxContainer();
                    choices.Name = "Choices";
                    choices.Visible = false;
                    vbox.AddChild(choices);
                    var footer = new HBoxContainer();
                    footer.Name = "Footer";
                    vbox.AddChild(footer);
                    var nextbtn = new Button();
                    nextbtn.Name = "NextButton";
                    nextbtn.Text = "次へ";
                    footer.AddChild(nextbtn);
                    nextbtn.Pressed += OnNextPressed;
                    AddChild(dialogBoxInstance);
                }
            }
        }
        if (dialogBoxInstance != null)
            dialogBoxInstance.Visible = true;
    }

    private void HideDialogBox()
    {
        if (dialogBoxInstance != null)
            dialogBoxInstance.Visible = false;
    }

    private void ShowCurrentNode()
    {
        if (currentNodeMap == null || string.IsNullOrEmpty(currentNodeId))
            return;
        if (!currentNodeMap.ContainsKey(currentNodeId))
            return;

        var nd = (Godot.Collections.Dictionary)currentNodeMap[currentNodeId];
        GD.Print("ShowCurrentNode: currentNodeId=", currentNodeId, " keys=", nd.Keys);

        // Diagnostic: print dialogBoxInstance and its child hierarchy to see why UI nodes aren't found
        if (dialogBoxInstance == null)
        {
            GD.PrintErr("ShowCurrentNode: dialogBoxInstance is NULL");
        }
        else
        {
            GD.Print("ShowCurrentNode: dialogBoxInstance present. Visible=", dialogBoxInstance.Visible, " Name=", dialogBoxInstance.Name);
            var topChildren = dialogBoxInstance.GetChildren();
            foreach (Node c in topChildren)
            {
                GD.Print(" - child: ", c.Name, " (", c.GetType(), ")");
                var sub = c.GetChildren();
                foreach (Node cc in sub)
                {
                    GD.Print("   - sub: ", cc.Name, " (", cc.GetType(), ")");
                    var sub2 = cc.GetChildren();
                    foreach (Node ccc in sub2)
                    {
                        GD.Print("     - sub2: ", ccc.Name, " (", ccc.GetType(), ")");
                    }
                }
            }
        }

        string type = nd.ContainsKey("type") ? (string)nd["type"] : "line";

        var speakerLabel = dialogBoxInstance?.GetNodeOrNull<Label>("Panel/VBox/Header/Speaker");
        var body = dialogBoxInstance?.GetNodeOrNull<RichTextLabel>("Panel/VBox/Body");
        var choices = dialogBoxInstance?.GetNodeOrNull<VBoxContainer>("Panel/VBox/Choices");
        var next = dialogBoxInstance?.GetNodeOrNull<Button>("Panel/VBox/Footer/NextButton");

        // Fallback: if nodes weren't found by exact path, try finding by name suffix (handles names like "DialogBox#Panel")
        if (speakerLabel == null)
        {
            speakerLabel = FindNodeByNameSuffix<Label>(dialogBoxInstance, "Speaker");
            if (speakerLabel != null)
                GD.Print("ShowCurrentNode: Speaker found by suffix: ", speakerLabel.Name);
        }
        if (body == null)
        {
            body = FindNodeByNameSuffix<RichTextLabel>(dialogBoxInstance, "Body");
            if (body != null)
                GD.Print("ShowCurrentNode: Body found by suffix: ", body.Name);
        }
        if (choices == null)
        {
            choices = FindNodeByNameSuffix<VBoxContainer>(dialogBoxInstance, "Choices");
            if (choices != null)
                GD.Print("ShowCurrentNode: Choices found by suffix: ", choices.Name);
        }
        if (next == null)
        {
            next = FindNodeByNameSuffix<Button>(dialogBoxInstance, "NextButton");
            if (next != null)
            {
                GD.Print("ShowCurrentNode: NextButton found by suffix: ", next.Name);
                try
                {
                    // Avoid duplicate connections by tracking instance IDs (ulong)
                    try
                    {
                        var id = next.GetInstanceId();
                        if (!connectedButtonIds.Contains(id))
                        {
                            next.Pressed += OnNextPressed;
                            connectedButtonIds.Add(id);
                        }
                    }
                    catch
                    {
                        // Fallback: attempt to connect once
                        try { next.Pressed += OnNextPressed; } catch { }
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr("Failed to connect NextButton safely: ", e.Message);
                }
            }
        }

        // reset choices
        if (choices != null)
        {
            var childList = choices.GetChildren();
            foreach (Node ch in childList)
                ch.QueueFree();
        }

        if (type == "line")
        {
            if (speakerLabel != null && nd.ContainsKey("speaker"))
            {
                speakerLabel.Text = (string)nd["speaker"];
                GD.Print("ShowCurrentNode: speaker set=", speakerLabel.Text);
                // Try to set a speaker image in the top area if available.
                try
                {
                    var speakerImage = dialogBoxInstance?.GetNodeOrNull<TextureRect>("SpeakerImage");
                    if (speakerImage == null)
                        speakerImage = FindNodeByNameSuffix<TextureRect>(dialogBoxInstance, "SpeakerImage");
                    if (speakerImage != null)
                    {
                        // hide by default
                        try { speakerImage.Visible = false; } catch { }
                        string imagePath = null;
                        if (nd.ContainsKey("speaker_image"))
                        {
                            imagePath = nd["speaker_image"].ToString();
                        }
                        else
                        {
                            // simple heuristic: if speaker name mentions 'uichi', show uichi front stand
                            try
                            {
                                var sp = ((string)nd["speaker"]).ToLower();
                                if (sp.Contains("uichi"))
                                    imagePath = "res://assets/characters/uichi/uichi-front-stand.png";
                            }
                            catch { }
                        }
                        // fallback: if no speaker_image provided, try using the NPC id (currentNpcId)
                        if (string.IsNullOrEmpty(imagePath) && !string.IsNullOrEmpty(currentNpcId))
                        {
                            try
                            {
                                var candidate = $"res://assets/characters/{currentNpcId}_standing_picture.png";
                                // only set if file exists
                                bool exists2 = false;
                                try { exists2 = FileAccess.FileExists(candidate); } catch { }
                                GD.Print($"DialogManager: Fallback candidate image='{candidate}' exists={exists2}");
                                if (exists2)
                                    imagePath = candidate;
                            }
                            catch { }
                        }
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            try
                            {
                                // diagnostic: check file exists
                                bool exists = false;
                                try { exists = FileAccess.FileExists(imagePath); } catch { }
                                GD.Print($"DialogManager: Speaker image path='{imagePath}' exists={exists}");
                                var tex = GD.Load<Texture2D>(imagePath);
                                GD.Print($"DialogManager: Load texture result for '{imagePath}': "+(tex!=null));
                                if (tex != null)
                                {
                                    speakerImage.Texture = tex;
                                    try { speakerImage.Visible = true; } catch { }
                                }
                                else
                                {
                                    GD.PrintErr("DialogManager: failed to load texture for speaker image: ", imagePath);
                                }
                            }
                            catch (Exception e)
                            {
                                GD.PrintErr("DialogManager: exception loading speaker image: ", e.Message);
                            }
                        }
                    }
                }
                catch { }
            }
            if (body != null && nd.ContainsKey("text"))
            {
                try
                {
                    // Ensure the body is visible and configured for plain text.
                    try { body.Visible = true; } catch { }
                    try { body.Show(); } catch { }
                    try { body.BbcodeEnabled = false; } catch { }
                    // Clear previous text and append new text to ensure visibility
                    body.Clear();
                    body.AppendText((string)nd["text"]);
                    // Attempt to reset scroll to top so text is immediately visible
                    try { body.ScrollToLine(0); } catch { }
                    // Fallback: if the RichTextLabel is too small in the scene, enforce a sensible minimum
                    try
                    {
                        // Try to read current rect size; fall back safely if property access differs across bindings
                        Vector2 rectSize = new Vector2();
                        try { rectSize = (Vector2)body.Get("rect_size"); } catch { rectSize = new Vector2(); }
                        if (rectSize.X < 120f || rectSize.Y < 32f)
                        {
                            try { body.Set("rect_min_size", new Vector2(240f, 64f)); } catch { body.Set("rect_min_size", new Vector2(240f, 64f)); }
                        }
                        // Ensure autowrap is enabled so long lines become visible
                        try { body.Set("autowrap", true); } catch { }
                    }
                    catch { }
                    // No explicit redraw call available; rely on the engine to refresh the control
                    GD.Print("ShowCurrentNode: body set=", (string)nd["text"]);
                }
                catch (Exception e)
                {
                    GD.PrintErr("Failed to set body text: ", e.Message);
                }
            }
            if (choices != null)
                choices.Visible = false;
            // Ensure any stray "次へ" buttons are visible when showing a line
            if (dialogBoxInstance != null)
                SetAllNextButtonsVisible(dialogBoxInstance, true);
            if (next != null)
            {
                GD.Print("ShowCurrentNode: setting Next visible. Before=", next.Visible, " Name=", next.Name);
                next.Visible = true;
                next.Show();
                    try { EnsureButtonInteractive(next); } catch { }
                GD.Print("ShowCurrentNode: Next after set visible=", next.Visible);
            }

            // apply once/set_flag
            if (nd.ContainsKey("once") && (bool)nd["once"] && !string.IsNullOrEmpty(currentNpcId))
                seen[currentNpcId] = true;
            if (nd.ContainsKey("set_flag"))
            {
                // not implemented: placeholder for future
            }

            // actions: e.g. give_item
            if (nd.ContainsKey("actions"))
            {
                var actions = (Godot.Collections.Array)nd["actions"];
                foreach (var a in actions)
                {
                    var ad = (Godot.Collections.Dictionary)a;
                    if (ad.ContainsKey("give_item"))
                    {
                        var itemId = (string)ad["give_item"];
                        try
                        {
                            // ensure GameState exists
                            GameState.EnsureInstance(GetTree());
                            if (GameState.Instance != null)
                            {
                                GameState.Instance.AddItem(itemId);
                                GD.Print($"DialogManager: give_item executed -> {itemId}");
                            }
                        }
                        catch (Exception e)
                        {
                            GD.PrintErr("DialogManager: give_item failed: ", e.Message);
                        }
                    }
                    if (ad.ContainsKey("set_flag"))
                    {
                        // placeholder for flag handling
                        GD.Print("DialogManager: set_flag: ", (string)ad["set_flag"]);
                    }
                }
            }
        }
        else if (type == "choice")
        {
            if (speakerLabel != null) speakerLabel.Text = "";
            if (body != null) body.Text = "";
            if (choices != null)
            {
                choices.Visible = true;
                var arr = (Godot.Collections.Array)nd["choices"];
                int idx = 0;
                foreach (var c in arr)
                {
                    var cd = (Godot.Collections.Dictionary)c;
                    var btn = new Button();
                    btn.Text = (string)cd["text"];
                    int captured = idx;
                    btn.Pressed += () => OnChoiceSelected(captured);
                    choices.AddChild(btn);
                    idx++;
                }
            }
            // Hide any Next buttons while choices are shown (covers duplicates/overlays)
            if (dialogBoxInstance != null)
                SetAllNextButtonsVisible(dialogBoxInstance, false);
        }
        else if (type == "end")
        {
            StopDialogue();
        }
    }

    private void OnNextPressed()
    {
        if (currentNodeMap == null || string.IsNullOrEmpty(currentNodeId))
            return;
        if (!currentNodeMap.ContainsKey(currentNodeId))
        {
            GD.PrintErr("OnNextPressed: currentNodeId not found in currentNodeMap: ", currentNodeId);
            StopDialogue();
            return;
        }
        var nd = (Godot.Collections.Dictionary)currentNodeMap[currentNodeId];
        if (nd.ContainsKey("next"))
        {
            var nextId = nd["next"].ToString();
            if (string.IsNullOrEmpty(nextId) || !currentNodeMap.ContainsKey(nextId))
            {
                GD.PrintErr("OnNextPressed: next id is missing or not found: ", nextId);
                StopDialogue();
                return;
            }
            currentNodeId = nextId;
            ShowCurrentNode();
        }
        else
        {
            StopDialogue();
        }
    }

    private void OnChoiceSelected(int index)
    {
        if (currentNodeMap == null || string.IsNullOrEmpty(currentNodeId) || !currentNodeMap.ContainsKey(currentNodeId))
        {
            GD.PrintErr("OnChoiceSelected: invalid currentNodeId or map");
            return;
        }
        var nd = (Godot.Collections.Dictionary)currentNodeMap[currentNodeId];
        if (!nd.ContainsKey("choices"))
        {
            GD.PrintErr("OnChoiceSelected: current node has no choices: ", currentNodeId);
            return;
        }
        var arr = (Godot.Collections.Array)nd["choices"];
        if (index < 0 || index >= arr.Count)
            return;
        var cd = (Godot.Collections.Dictionary)arr[index];
        if (cd.ContainsKey("next"))
        {
            var nextId = (string)cd["next"];
            if (string.IsNullOrEmpty(nextId) || !currentNodeMap.ContainsKey(nextId))
            {
                GD.PrintErr("OnChoiceSelected: next id invalid: ", nextId);
                StopDialogue();
                return;
            }
            currentNodeId = nextId;
        }
        else
            currentNodeId = null;
        ShowCurrentNode();
    }

    public void StopDialogue()
    {
        // free the dialog instance so it won't remain in the scene tree
        if (dialogBoxInstance != null)
        {
            try
            {
                // unregister connected buttons under this dialog to avoid leftover ids
                try
                {
                    var btns = dialogBoxInstance.GetChildren();
                    foreach (Node c in btns)
                    {
                        // walk children recursively and remove button ids
                        UnregisterButtonsRecursively(c);
                    }
                }
                catch { }
                dialogBoxInstance.QueueFree();
            }
            catch { }
            dialogBoxInstance = null;
        }
        // restore paused state if we paused the tree for the dialog
        if (pausedByDialog)
        {
            try { GetTree().Paused = false; } catch { }
            pausedByDialog = false;
        }
        currentNodeMap = null;
        currentNodeId = null;
        currentNpcId = null;
        nodes = null;
    }

    private void UnregisterButtonsRecursively(Node root)
    {
        if (root == null) return;
        if (root is Button b)
        {
            try { connectedButtonIds.Remove(b.GetInstanceId()); } catch { }
        }
        var children = root.GetChildren();
        foreach (Node c in children)
            UnregisterButtonsRecursively(c);
    }

    public bool IsActive()
    {
        return currentNodeMap != null;
    }

    // Helper: recursively find a node whose name equals or ends with the given suffix and cast to T
    private T FindNodeByNameSuffix<T>(Node root, string suffix) where T : Node
    {
        if (root == null)
            return null;
        var children = root.GetChildren();
        foreach (Node c in children)
        {
            var name = c.Name?.ToString();
            if (!string.IsNullOrEmpty(name) && (name == suffix || name.EndsWith("#" + suffix) || name.EndsWith(suffix)))
            {
                if (c is T tnode)
                    return tnode;
            }
            var found = FindNodeByNameSuffix<T>(c, suffix);
            if (found != null)
                return found;
        }
        return null;
    }

    // Helper: set visibility for all Buttons with text "次へ" under a root node
    private void SetAllNextButtonsVisible(Node root, bool visible)
    {
        if (root == null) return;
        var children = root.GetChildren();
        foreach (Node c in children)
        {
            if (c is Button b)
            {
                try
                {
                    if (b.Text == "次へ")
                    {
                        b.Visible = visible;
                        if (visible) b.Show(); else b.Hide();
                    }
                }
                catch { }
            }
            SetAllNextButtonsVisible(c, visible);
        }
    }
}
