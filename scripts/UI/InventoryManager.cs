using Godot;
using System;
using System.Text.Json;

public partial class InventoryManager : Node
{
    private Node panelRoot;
    private Panel inventoryPanel;
    private VBoxContainer itemList;
    private bool gameStateConnected = false;
    private bool isRefreshing = false; // prevent re-entrant or duplicate refreshes
    private PackedScene itemEntryScene = null;
    // simple in-memory metadata map: itemId -> { displayName, icon }
    private Godot.Collections.Dictionary itemMetadata = new Godot.Collections.Dictionary();

    public override void _Ready()
    {
        // Try to load the UI scene
        var ps = GD.Load<PackedScene>("res://scenes/ui/InventoryPanel.tscn");
        if (ps != null)
        {
            panelRoot = ps.Instantiate();
            panelRoot.Name = "InventoryPanelRoot";
            // Defer adding and initializing the panel to avoid "parent is busy setting up children" errors.
            GetTree().Root.CallDeferred("add_child", panelRoot);
            CallDeferred("DeferredInitPanel");
            // load entry scene for each item row
            try { itemEntryScene = GD.Load<PackedScene>("res://scenes/ui/InventoryItemEntry.tscn"); } catch { itemEntryScene = null; }
            // populate basic metadata (extend as needed)
            try
            {
                // Try to load centralized item definitions from JSON
                string itemsJsonPath = "res://assets/items/items.json";
                if (FileAccess.FileExists(itemsJsonPath))
                {
                    try
                    {
                        using (var fa = FileAccess.Open(itemsJsonPath, FileAccess.ModeFlags.Read))
                        {
                            var txt = fa.GetAsText();
                            if (!string.IsNullOrEmpty(txt))
                            {
                                try
                                {
                                    using (var doc = JsonDocument.Parse(txt))
                                    {
                                        var root = doc.RootElement;
                                        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out var itemsArr) && itemsArr.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var el in itemsArr.EnumerateArray())
                                            {
                                                try
                                                {
                                                    if (!el.TryGetProperty("id", out var idEl)) continue;
                                                    var id = idEl.GetString();
                                                    if (string.IsNullOrEmpty(id)) continue;
                                                    var displayName = el.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? id : id;
                                                    var icon = el.TryGetProperty("icon", out var ic) ? ic.GetString() ?? string.Empty : string.Empty;
                                                    var md = new Godot.Collections.Dictionary();
                                                    md["displayName"] = displayName;
                                                    md["icon"] = icon;
                                                    itemMetadata[id] = md;
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                                catch (Exception je) { GD.PrintErr($"InventoryManager: items.json parse failed: {je.Message}"); }
                            }
                        }
                    }
                    catch (Exception fe) { GD.PrintErr($"InventoryManager: failed to read items.json: {fe.Message}"); }
                }

                // Fallback defaults if JSON didn't provide them
                if (!itemMetadata.ContainsKey("material_sample"))
                {
                    var m = new Godot.Collections.Dictionary();
                    m["displayName"] = "素材サンプル";
                    m["icon"] = "res://assets/items/material_sample.png";
                    itemMetadata["material_sample"] = m;
                }

                if (!itemMetadata.ContainsKey("taro_chan_material"))
                {
                    var m2 = new Godot.Collections.Dictionary();
                    m2["displayName"] = "たろちゃんの素材";
                    m2["icon"] = "res://assets/items/taro_chan_mrterial.png";
                    itemMetadata["taro_chan_material"] = m2;
                }
            }
            catch (Exception e) { GD.PrintErr($"InventoryManager: failed to populate itemMetadata: {e.Message}"); }
        }
        else
        {
            GD.Print("InventoryManager: InventoryPanel.tscn not found.");
        }

        // Connect to GameState signal if available (can be null in some startup orders)
        if (GameState.Instance != null)
        {
            GameState.Instance.Connect("ItemAdded", new Callable(this, "OnItemAdded"));
            gameStateConnected = true;
        }
    }

    private void DeferredInitPanel()
    {
        if (panelRoot == null) return;
        // Now it's safe to get child nodes
        try
        {
            GD.Print("InventoryManager: DeferredInitPanel starting search for Panel/VBoxContainer");

            inventoryPanel = FindFirstOfType<Panel>(panelRoot);
            if (inventoryPanel != null)
                GD.Print($"InventoryManager: Found Panel node: {inventoryPanel.Name}");
            else
                GD.PrintErr("InventoryManager: Panel node not found under panelRoot");

            // Prefer a VBoxContainer that is a child of the panel; if not found, search entire panelRoot.
            if (inventoryPanel != null)
                itemList = FindFirstOfType<VBoxContainer>(inventoryPanel);

            if (itemList == null)
                itemList = FindFirstOfType<VBoxContainer>(panelRoot);

            if (itemList != null)
                GD.Print($"InventoryManager: Found ItemList VBoxContainer: {itemList.Name}");
            else
                GD.PrintErr("InventoryManager: ItemList VBoxContainer not found under panelRoot");

            if (inventoryPanel != null)
            {
                // Make panel background darker to improve contrast with labels
                inventoryPanel.Modulate = new Color(0, 0, 0, 0.75f);
                inventoryPanel.Visible = false;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"InventoryManager: DeferredInitPanel failed: {e.Message}");
        }
    }

    private T FindFirstOfType<T>(Node root) where T : Node
    {
        if (root == null) return null;
        foreach (Node c in root.GetChildren())
        {
            if (c is T t) return t;
            var found = FindFirstOfType<T>(c);
            if (found != null) return found;
        }
        return null;
    }

    public override void _Process(double delta)
    {
        try
        {
            if (Input.IsActionJustPressed("inventory"))
                ToggleInventory();
        }
        catch { }
    }

    private void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        inventoryPanel.Visible = !inventoryPanel.Visible;
        if (inventoryPanel.Visible)
        {
            RefreshInventory();
        }
        else
        {
            // on hide, clear existing entries so visuals don't persist
            if (itemList != null)
            {
                var children = itemList.GetChildren();
                foreach (Node c in children)
                {
                    try { itemList.RemoveChild(c); } catch { }
                    try { c.QueueFree(); } catch { }
                }
            }
        }
    }

    private void RefreshInventory()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        try
        {
            if (itemList == null) return;
            // clear existing: remove immediately from container then queue free to avoid duplicates
            var existing = itemList.GetChildren();
            foreach (Node child in existing)
            {
                try { itemList.RemoveChild(child); } catch { }
                try { child.QueueFree(); } catch { }
            }

        if (GameState.Instance == null)
        {
            GD.Print("InventoryManager: GameState.Instance is null when RefreshInventory called.");
            var emptyLbl = new Label();
            emptyLbl.Name = "EmptyInventoryLabel";
            emptyLbl.Text = "所持品が見つかりません";
            itemList.AddChild(emptyLbl);
            return;
        }

        // Ensure we're connected to ItemAdded signal if Instance appeared later
        if (!gameStateConnected)
        {
            try
            {
                GameState.Instance.Connect("ItemAdded", new Callable(this, "OnItemAdded"));
                gameStateConnected = true;
            }
            catch (Exception e)
            {
                GD.PrintErr($"InventoryManager: failed to connect to GameState.ItemAdded: {e.Message}");
            }
        }

        var inv = GameState.Instance.GetInventoryCopy();
        GD.Print($"InventoryManager: RefreshInventory, items count={inv.Count}");
        if (inv.Count == 0)
        {
            var empty = new Label();
            empty.Name = "EmptyInventoryLabel";
            empty.Text = "所持品はありません";
            itemList.AddChild(empty);
            return;
        }

        foreach (var kv in inv)
        {
            string itemId = (string)kv.Key;
            int count = (int)kv.Value;
            // Try to instantiate a nicer entry scene if available
            if (itemEntryScene != null)
            {
                try
                {
                    var entry = itemEntryScene.Instantiate();
                    if (entry is Node en)
                    {
                        // find child controls
                        TextureRect icon = en.GetNodeOrNull<TextureRect>("Icon");
                        Label nameLbl = en.GetNodeOrNull<Label>("NameLabel");
                        Label countLbl = en.GetNodeOrNull<Label>("CountLabel");

                        string displayName = itemId;
                        string iconPath = null;
                        try
                        {
                            if (itemMetadata.ContainsKey(itemId))
                            {
                                var md = (Godot.Collections.Dictionary)itemMetadata[itemId];
                                if (md.ContainsKey("displayName")) displayName = (string)md["displayName"];
                                if (md.ContainsKey("icon")) iconPath = (string)md["icon"];
                            }
                        }
                        catch { }

                        if (nameLbl != null) nameLbl.Text = displayName;
                        if (countLbl != null) countLbl.Text = $"x{count}";

                        if (icon != null && !string.IsNullOrEmpty(iconPath))
                        {
                            try
                            {
                                bool exists = false;
                                try { exists = FileAccess.FileExists(iconPath); } catch { }
                                if (exists)
                                {
                                    var tex = GD.Load<Texture2D>(iconPath);
                                    if (tex != null) icon.Texture = tex;
                                }
                            }
                            catch { }
                        }

                        itemList.AddChild(en);
                        GD.Print($"InventoryManager: added entry for {itemId} name='{displayName}' count={count}");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"InventoryManager: failed to instantiate item entry: {e.Message}");
                }
            }

            // fallback: plain label
            var lbl = new Label();
            lbl.Name = $"Item_{itemId}";
            lbl.Text = $"{itemId} x{count}";
            lbl.Visible = true;
            try { lbl.AddThemeColorOverride("font_color", new Color(1,1,1)); } catch { }
            try { lbl.Show(); itemList.AddChild(lbl); GD.Print($"InventoryManager: added label {lbl.Name} text='{lbl.Text}'"); } catch (Exception e) { GD.PrintErr($"InventoryManager: failed to add label {lbl.Name}: {e.Message}"); }
        }

        // Debug: list children of itemList
        GD.Print($"InventoryManager: itemList child_count={itemList.GetChildCount()}");
        for (int i = 0; i < itemList.GetChildCount(); i++)
        {
            var c = itemList.GetChild(i);
            if (c is CanvasItem ci)
                GD.Print($"InventoryManager: itemList child[{i}] = {c.Name} ({c.GetType().Name}) Visible={ci.Visible}");
            else
                GD.Print($"InventoryManager: itemList child[{i}] = {c.Name} ({c.GetType().Name}) Visible=N/A");
        }
        }
        finally
        {
            isRefreshing = false;
        }
    }

    // Called when GameState emits ItemAdded (itemId, newCount)
    public void OnItemAdded(string itemId, int newCount)
    {
        if (inventoryPanel != null && inventoryPanel.Visible)
            RefreshInventory();
    }
}
