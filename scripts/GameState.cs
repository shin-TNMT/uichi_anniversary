using Godot;
using System;
using Godot.Collections;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    // inventory: item_id -> count
    private Godot.Collections.Dictionary<string, int> inventory = new Godot.Collections.Dictionary<string, int>();
    // npcGiven: npc_id -> set of item_ids this npc has given (to prevent repeated gives per-NPC)
    private Godot.Collections.Dictionary<string, Godot.Collections.Array> npcGiven = new Godot.Collections.Dictionary<string, Godot.Collections.Array>();

    // Win threshold (number of distinct or total items required). Use total count for simplicity.
    [Export]
    public int winThreshold = 14;

    [Signal]
    public delegate void ItemAddedEventHandler(string itemId, int newCount);

    [Signal]
    public delegate void VictoryEventHandler();

    public override void _Ready()
    {
        Instance = this;
        GD.Print("GameState: Ready. winThreshold=", winThreshold);
    }

    public static void EnsureInstance(SceneTree tree)
    {
        if (Instance == null)
        {
            var gs = new GameState();
            gs.Name = "GameState";
            tree.Root.AddChild(gs);
            // _Ready will set Instance
        }
    }

    public void AddItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (!inventory.ContainsKey(itemId))
            inventory[itemId] = 0;
        inventory[itemId] += count;
        var newCount = inventory[itemId];
        GD.Print($"GameState: AddItem {itemId} -> {newCount}");
        // Godot C# signal names are the delegate name without the trailing 'EventHandler'
        EmitSignal(nameof(ItemAddedEventHandler).Replace("EventHandler", ""), itemId, newCount);
        CheckVictory();
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return inventory.ContainsKey(itemId) ? inventory[itemId] : 0;
    }

    public int GetTotalItemCount()
    {
        int sum = 0;
        foreach (var kv in inventory)
            sum += kv.Value;
        return sum;
    }

    // Return a shallow copy of the inventory for safe read-only use by UI
    public Godot.Collections.Dictionary<string, int> GetInventoryCopy()
    {
        var copy = new Godot.Collections.Dictionary<string, int>();
        foreach (var kv in inventory)
            copy[(string)kv.Key] = (int)kv.Value;
        return copy;
    }

    // NPC-specific give tracking
    public bool HasNpcGiven(string npcId, string itemId)
    {
        if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(itemId)) return false;
        if (!npcGiven.ContainsKey(npcId)) return false;
        var arr = npcGiven[npcId];
        return arr.Contains(itemId);
    }

    public void MarkNpcGiven(string npcId, string itemId)
    {
        if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(itemId)) return;
        if (!npcGiven.ContainsKey(npcId))
            npcGiven[npcId] = new Godot.Collections.Array();
        var arr = npcGiven[npcId];
        if (!arr.Contains(itemId))
            arr.Add(itemId);
    }

    private void CheckVictory()
    {
        var total = GetTotalItemCount();
        if (total >= winThreshold)
        {
            GD.Print("GameState: Victory achieved! total=", total);
            EmitSignal(nameof(VictoryEventHandler).Replace("EventHandler", ""));
            // Simple visual: create a Popup at root
            ShowWinPopup();
        }
    }

    private void ShowWinPopup()
    {
        // Create a simple Panel popup (avoid engine-specific WindowDialog types)
        var panel = new Panel();
        panel.Name = "WinPopup";
        panel.Modulate = new Color(1,1,1,0.95f);
        var v = new VBoxContainer();
    v.Name = "WinVBox";
    var lbl = new Label();
    lbl.Text = "必要なアイテムを集めました！ お店を開けます。";
        v.AddChild(lbl);
        var btn = new Button();
        btn.Text = "閉じる";
        btn.Pressed += () => { panel.QueueFree(); };
        v.AddChild(btn);
        panel.AddChild(v);
        GetTree().Root.AddChild(panel);
    // (simple) leave positioning to the scene root / user; panel will appear as added child
    }
}
