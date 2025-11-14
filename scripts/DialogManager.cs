using Godot;
using System;
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
                        GetTree().Root.AddChild(hostControl);
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
                            GetTree().Root.AddChild(wrapper);
                        }
                        else
                        {
                            // add the original instance to the root so child control is in the tree
                            GetTree().Root.AddChild(instW);
                        }
                    }

                    if (hostControl != null)
                    {
                        dialogBoxInstance = hostControl;
                        dialogBoxInstance.Name = "DialogWindow";
                        // Pause the tree so gameplay input stops while dialog is open
                        GetTree().Paused = true;
                        pausedByDialog = true;
                        // If WindowDialog, popup centered (use dynamic call to avoid type dependency)
                        try { dialogBoxInstance.Call("popup_centered"); } catch { }
                        // connect Next button if present
                        var next = dialogBoxInstance.GetNodeOrNull<Button>("Panel/VBox/Footer/NextButton");
                        if (next != null)
                            next.Pressed += OnNextPressed;
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
                            next.Pressed += OnNextPressed;
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
                    // ensure the Next button is connected - removal can throw if not connected, so guard it
                    try { next.Pressed -= OnNextPressed; } catch { }
                    try { next.Pressed += OnNextPressed; } catch (Exception e) { GD.PrintErr("Failed to connect NextButton: ", e.Message); }
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
            }
            if (body != null && nd.ContainsKey("text"))
            {
                try
                {
                    // Clear previous text and append new text to ensure visibility
                    body.Clear();
                    body.AppendText((string)nd["text"]);
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
