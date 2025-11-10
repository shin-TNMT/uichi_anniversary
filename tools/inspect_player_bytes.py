p = r'C:\Users\owner\Documents\codes\uichi_anniversary\scenes\player\Player.tscn'
with open(p, 'rb') as f:
    data = f.read()
print('total bytes:', len(data))
# splitlines keeps linebreaks? we use split(b'\n')
lines = data.split(b'\n')
for i, line in enumerate(lines, start=1):
    hexs = ' '.join(f'{b:02x}' for b in line)
    try:
        txt = line.decode('utf-8')
    except Exception:
        txt = line.decode('utf-8', 'replace')
    print(f'{i:03}: bytes={len(line):3} hex=[{hexs}] text={repr(txt)}')

# print specific line 16 if exists
if len(lines) >= 16:
    l = lines[15]
    print('\n--- LINE 16 DETAIL ---')
    print('raw bytes:', l)
    print('hex:', ' '.join(f'{b:02x}' for b in l))
    print('decoded:', l.decode('utf-8', 'replace'))
