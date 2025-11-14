@tool
extends EditorScript

# EditorScript: TownTiles.tres のバックアップを作り、TileSet リソースをエディタで開きます。
# 実行は Godot エディタ内で行ってください: Project -> Tools -> Execute Editor Script
func _run():
    var tile_path := "res://scenes/tiles/TownTiles.tres"
    var backup_path := tile_path + ".backup"

    # バックアップを作成（既にあれば上書きしない）
    if FileAccess.file_exists(backup_path):
        print("[set_tileset_autotile] Backup already exists: %s" % backup_path)
    else:
        var orig := FileAccess.open(tile_path, FileAccess.ModeFlags.READ)
        if orig:
            var data := orig.get_buffer(orig.get_length())
            orig.close()
            var out := FileAccess.open(backup_path, FileAccess.ModeFlags.WRITE)
            if out:
                out.store_buffer(data)
                out.close()
                print("[set_tileset_autotile] Backup created: %s" % backup_path)
        else:
            printerr("[set_tileset_autotile] Failed to open original TileSet: %s" % tile_path)
            return

    # リソースをロードしてエディタで開く
    var res = ResourceLoader.load(tile_path)
    if not res:
        printerr("[set_tileset_autotile] Failed to load TileSet resource: %s" % tile_path)
        return

    var ei = get_editor_interface()
    if ei:
        EditorInterface.edit_resource(res)
        print("[set_tileset_autotile] Opened TileSet in editor: %s" % tile_path)
        _print_next_steps()
    else:
        printerr("[set_tileset_autotile] Cannot get editor interface. Run inside Godot Editor.")

func _print_next_steps():
    print("--- 次の手順（エディタ内で実行） ---")
    print("1) TileSet の 'Atlas Sources' を選択し、'Texture region size' が Vector2i(32,32) になっていることを確認してください.")
    print("2) 必要なら Atlas を選んで 'Create Autotile' を押し、床タイル (例: Tile ID 0) を Autotile に設定します.")
    print("3) 各タイルに Collision を追加するには、タイルを選択 → 'Collision' タブ → 'Add Shape' → Rectangle を追加して保存してください.")
    print("4) 変更を保存して TileMap をプレビューしてください（scenes/Town.tscn を開いて確認）")
    print("--- スクリプトはここまで ---")
