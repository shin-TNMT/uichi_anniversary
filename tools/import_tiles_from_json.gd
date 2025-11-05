@tool
extends EditorScript

func _run():
    var json_path = "res://scenes/tiles/TownTiles_slices.json"
    var out_tres = "res://scenes/tiles/TownTiles.tres"

    var fa = FileAccess.open(json_path, FileAccess.READ)
    if fa == null:
        printerr("Cannot open ", json_path)
        return
    var txt = fa.get_as_text()
    fa.close()

    var parsed = JSON.parse_string(txt)
    # JSON.parse_string may return a Dictionary containing {"result":..., "error":...}
    # or directly the parsed data depending on environment/version. Handle both.
    var data = null
    if typeof(parsed) == TYPE_DICTIONARY and parsed.has("result"):
        data = parsed.get("result")
    elif typeof(parsed) == TYPE_DICTIONARY:
        data = parsed
    else:
        printerr("JSON parse returned unexpected type: ", typeof(parsed))
        return

    var tex = load(data.get("source"))
    if tex == null:
        printerr("Cannot load texture: ", data.get("source"))
        return

    var tile_size = int(data.get("tile_size", 48))
    var cols = int(data.get("cols", 1))
    var rows = int(data.get("rows", 1))

    var tileset = TileSet.new()
    var id = 0
    # Some Godot versions expose TileSet APIs differently. If create_tile is available, use it.
    if tileset.has_method("create_tile"):
        for r in range(rows):
            for c in range(cols):
                var rect = Rect2(c * tile_size, r * tile_size, tile_size, tile_size)
                # create tile and assign texture region
                tileset.create_tile(id)
                tileset.tile_set_texture(id, tex)
                tileset.tile_set_region(id, rect)
                id += 1

        var err = ResourceSaver.save(out_tres, tileset)
        if err != OK:
            printerr("Failed to save TileSet: ", err)
        else:
            print("Saved TileSet: ", out_tres, " (tiles: ", id, ")")
    else:
        # Fallback: save a minimal .tres that references the texture so the user can open it in the editor
        var content = "[gd_resource type=\"TileSet\" load_steps=1 format=3]\n"
        content += "[ext_resource path=\"" + data.get("source") + "\" type=\"Texture2D\" id=1]\n\n"
        content += "# Tile definitions were not created automatically. Open this TileSet in the Godot editor and add tiles/Autotile rules using the referenced texture.\n"

        var fa_out = FileAccess.open(out_tres, FileAccess.WRITE)
        if fa_out == null:
            printerr("Failed to open output file for writing: ", out_tres)
            return
        fa_out.store_string(content)
        fa_out.close()
        print("Saved TileSet placeholder: ", out_tres)
