@tool
extends EditorScript

# Editor tool to fill a rectangular area of a TileMap/TileMapLayer in the opened scene.
# Usage:
# 1. Open the scene you want to modify in the editor.
# 2. Open Script Editor -> 'Run' -> "Run Editor Script..." and choose this script.
# 3. Configure the exported fields here in the file or edit the exports in the Inspector when the script is selected in the EditorScript panel.

@export var tilemap_node_path: NodePath = NodePath("")
@export var tile_id: int = 0
@export var start_pos: Vector2i = Vector2i.ZERO
@export var end_pos: Vector2i = Vector2i(10, 10)
@export var clear_first: bool = false

func _run():
	var root = EditorInterface.get_edited_scene_root()
	if root == null:
		printerr("No edited scene open. Open the scene you want to modify and run this script again.")
		return

	var tm = null
	if tilemap_node_path != NodePath(""):
		tm = root.get_node_or_null(tilemap_node_path)
		if tm == null:
			# Warn and fallback to recursive search if the explicit path wasn't found
			printerr("TileMap path '%s' not found from scene root; falling back to recursive search." % tilemap_node_path)
	if tm == null:
		# recursive fallback: find first TileMap / TileMapLayer in scene
		var stack = [root]
		while stack.size() > 0:
			var node = stack.pop_back()
			if node is TileMap or node is TileMapLayer:
				tm = node
				break
			for c in node.get_children():
				stack.push_back(c)

	if tm == null:
		printerr("TileMap/TileMapLayer not found at path: %s" % tilemap_node_path)
		return

	# Normalize rectangle
	var minx = min(start_pos.x, end_pos.x)
	var maxx = max(start_pos.x, end_pos.x)
	var miny = min(start_pos.y, end_pos.y)
	var maxy = max(start_pos.y, end_pos.y)

	if clear_first:
		# Attempt to clear existing cells in the area
		for x in range(minx, maxx + 1):
			for y in range(miny, maxy + 1):
				_set_cell(tm, Vector2i(x, y), -1)

	for x in range(minx, maxx + 1):
		for y in range(miny, maxy + 1):
			_set_cell(tm, Vector2i(x, y), tile_id)

	# Note: some EditorInterface APIs vary by Godot version. Ask user to save the scene manually.
	print("Fill complete (scene modified). Please save the scene in the editor. node=%s tile_id=%d area=(%d,%d)-(%d,%d)" % [tilemap_node_path, tile_id, minx, miny, maxx, maxy])

func _set_cell(tm, cell: Vector2i, id: int) -> void:
	# Prefer the explicit 'set_cell_item' API when present.
	if tm.has_method("set_cell_item"):
		tm.call("set_cell_item", cell, id)
		return

	# If method list introspection is available, try to pick the right signature.
	if tm.has_method("get_method_list"):
		var mlist = tm.get_method_list()
		for m in mlist:
			if typeof(m) == TYPE_DICTIONARY and m.has("name") and str(m["name"]) == "set_cell":
				var argc = 0
				if m.has("arguments"):
					argc = m["arguments"].size()
				if argc == 2:
					# Check if second argument expects a Vector2i (atlas coords)
					var second = m["arguments"][1] if m["arguments"].size() > 1 else null
					if typeof(second) == TYPE_DICTIONARY and second.has("type") and str(second["type"]).find("Vector2i") != -1:
						tm.call("set_cell", cell, Vector2i(id, 0))
						return
					else:
						tm.call("set_cell", cell, id)
						return
				elif argc == 3:
					# (int x, int y, int id)
					tm.call("set_cell", cell.x, cell.y, id)
					return

	# Fallback: try a few sensible calls if introspection isn't available.
	if tm.has_method("set_cell"):
		tm.call("set_cell", cell, id)
		return
	if tm.has_method("set_cellv"):
		tm.call("set_cellv", cell, id)
		return

	printerr("Failed to set cell on node of type %s. No recognized set_cell method found." % tm.get_class())
