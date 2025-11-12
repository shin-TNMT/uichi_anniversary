using Godot;
using System;
using Godot.Collections;

public partial class DialogManager : Node
{
    private Control dialogBoxInstance;
    private PackedScene dialogBoxScene;
    private Godot.Collections.Dictionary<string, bool> seen = new Godot.Collections.Dictionary<string, bool>();

    private Godot.Collections.Array nodes = null;
    private string currentNpcId = null;
    private Godot.Collections.Dictionary currentNodeMap = null; // id -> node dict
    private string currentNodeId = null;

    public override void _Ready()
    {
        // preload DialogBox scene
        dialogBoxScene = GD.Load<PackedScene>("res://scenes/ui/DialogBox.tscn");
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
        if (dialogBoxInstance == null && dialogBoxScene != null)
        {
            dialogBoxInstance = (Control)dialogBoxScene.Instantiate();
            AddChild(dialogBoxInstance);
            dialogBoxInstance.Name = "DialogBox";
            // connect Next button
            var next = dialogBoxInstance.GetNodeOrNull<Button>("Panel/VBox/Footer/NextButton");
            if (next != null)
                next.Pressed += OnNextPressed;
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
        string type = nd.ContainsKey("type") ? (string)nd["type"] : "line";

        var speakerLabel = dialogBoxInstance.GetNodeOrNull<Label>("Panel/VBox/Header/Speaker");
        var body = dialogBoxInstance.GetNodeOrNull<RichTextLabel>("Panel/VBox/Body");
        var choices = dialogBoxInstance.GetNodeOrNull<VBoxContainer>("Panel/VBox/Choices");
        var next = dialogBoxInstance.GetNodeOrNull<Button>("Panel/VBox/Footer/NextButton");

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
                speakerLabel.Text = (string)nd["speaker"];
            if (body != null && nd.ContainsKey("text"))
                body.Text = (string)nd["text"];
            if (choices != null)
                choices.Visible = false;
            if (next != null)
                next.Visible = true;

            // apply once/set_flag
            if (nd.ContainsKey("once") && (bool)nd["once"] && !string.IsNullOrEmpty(currentNpcId))
                seen[currentNpcId] = true;
            if (nd.ContainsKey("set_flag"))
            {
                // not implemented: placeholder for future
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
            if (next != null)
                next.Visible = false;
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
        var nd = (Godot.Collections.Dictionary)currentNodeMap[currentNodeId];
        if (nd.ContainsKey("next"))
        {
            currentNodeId = nd["next"].ToString();
            ShowCurrentNode();
        }
        else
        {
            StopDialogue();
        }
    }

    private void OnChoiceSelected(int index)
    {
        var nd = (Godot.Collections.Dictionary)currentNodeMap[currentNodeId];
        var arr = (Godot.Collections.Array)nd["choices"];
        if (index < 0 || index >= arr.Count)
            return;
        var cd = (Godot.Collections.Dictionary)arr[index];
        if (cd.ContainsKey("next"))
            currentNodeId = (string)cd["next"];
        else
            currentNodeId = null;
        ShowCurrentNode();
    }

    public void StopDialogue()
    {
        HideDialogBox();
        currentNodeMap = null;
        currentNodeId = null;
        currentNpcId = null;
        nodes = null;
    }

    public bool IsActive()
    {
        return currentNodeMap != null;
    }
}
