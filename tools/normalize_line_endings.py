p = r'C:\Users\owner\Documents\codes\uichi_anniversary\scenes\player\Player.tscn'
with open(p, 'rb') as f:
    data = f.read()
# Normalize CRLF -> LF, then CR -> LF
data = data.replace(b'\r\n', b'\n')
data = data.replace(b'\r', b'\n')
with open(p, 'wb') as f:
    f.write(data)
print('normalized, bytes:', len(data))
