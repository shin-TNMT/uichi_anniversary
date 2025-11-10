content = '''[gd_scene load_steps=2 format=4]

[ext_resource path="res://scripts/Player.cs" type="Script" id=1]
[ext_resource path="res://assets/characters/mendako.png" type="Texture2D" id=2]

[node name="Player" type="CharacterBody2D"]
script = ExtResource(1)

[node name="Sprite" type="Sprite2D" parent="Player"]
texture = ExtResource(2)
centered = true
position = Vector2(0, -16)

[node name="CollisionShape2D" type="CollisionShape2D" parent="Player"]
position = Vector2(0, -8)
shape = CircleShape2D {
  radius = 8.0
}
'''
with open(r'C:\Users\owner\Documents\codes\uichi_anniversary\scenes\player\Player.tscn','w',encoding='utf-8') as f:
    f.write(content)
print('written')
