scenes = {
    r'C:\Users\owner\Documents\codes\uichi_anniversary\scenes\Main.tscn': '''[gd_scene load_steps=2 format=3]

[ext_resource path="res://scripts/Main.cs" type="Script" id=1]

[node name="Main" type="Node2D"]
script = ExtResource(1)
''',
    r'C:\Users\owner\Documents\codes\uichi_anniversary\scenes\StartScreen.tscn': '''[gd_scene load_steps=2 format=3]

[ext_resource path="res://scripts/StartScreen.cs" type="Script" id=1]

[node name="StartScreen" type="CanvasLayer"]
script = ExtResource(1)

[node name="VBox" type="VBoxContainer" parent="."]
anchors_preset = -1
anchor_left = 0.25
anchor_top = 0.25
anchor_right = 0.75
anchor_bottom = 0.75

[node name="Title" type="Label" parent="VBox"]
layout_mode = 2
text = "Uichi Anniversary"
horizontal_alignment = 1

[node name="StartButton" type="Button" parent="VBox"]
layout_mode = 2
text = "Start"

[node name="OptionsButton" type="Button" parent="VBox"]
layout_mode = 2
text = "Options"

[node name="QuitButton" type="Button" parent="VBox"]
layout_mode = 2
text = "Quit"

[node name="OptionsDialog" type="Panel" parent="."]
visible = false
anchors_preset = -1
anchor_left = 0.15
anchor_top = 0.12
anchor_right = 0.85
anchor_bottom = 0.78

[node name="Margin" type="MarginContainer" parent="OptionsDialog"]
layout_mode = 0
offset_right = 485.2
offset_bottom = 85.0

[node name="VBoxContainer" type="VBoxContainer" parent="OptionsDialog/Margin"]
layout_mode = 2

[node name="OptionsLabel" type="Label" parent="OptionsDialog/Margin/VBoxContainer"]
layout_mode = 2
text = "Audio"

[node name="HBox" type="HBoxContainer" parent="OptionsDialog/Margin/VBoxContainer"]
layout_mode = 2

[node name="VolumeLabel" type="Label" parent="OptionsDialog/Margin/VBoxContainer/HBox"]
layout_mode = 2
size_flags_horizontal = 0
text = "Volume"

[node name="VolumeSlider" type="HSlider" parent="OptionsDialog/Margin/VBoxContainer/HBox"]
layout_mode = 2
size_flags_horizontal = 3
value = 100.0

[node name="VolumeValue" type="Label" parent="OptionsDialog/Margin/VBoxContainer/HBox"]
layout_mode = 2
size_flags_horizontal = 0
text = "100%"

[node name="CloseButton" type="Button" parent="OptionsDialog/Margin/VBoxContainer"]
layout_mode = 2
text = "Close"
'''
}

for path, content in scenes.items():
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print('wrote', path)
