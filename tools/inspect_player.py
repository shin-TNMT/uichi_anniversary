p = r'C:\Users\owner\Documents\codes\uichi_anniversary\scenes\player\Player.tscn'
with open(p, 'rb') as f:
    data = f.read()
print('bytes:', len(data))
try:
    s = data.decode('utf-8')
except Exception:
    s = data.decode('utf-8', 'replace')
lines = s.splitlines()
for i, l in enumerate(lines, start=1):
    print(f'{i:03}:', repr(l))
