import os, re, shutil, datetime
root = r'C:\Users\owner\Documents\codes\uichi_anniversary'
patterns = [r'uid="uid://[^"]+"']

modified = []

for dirpath, dirnames, filenames in os.walk(root):
    for fname in filenames:
        if not (fname.endswith('.tscn') or fname.endswith('.tres')):
            continue
        if '.bak' in fname or fname.endswith('.import'):
            continue
        path = os.path.join(dirpath, fname)
        with open(path, 'r', encoding='utf-8', errors='replace') as f:
            txt = f.read()
        orig = txt
        # Backup
        bak = path + '.bak.' + datetime.datetime.now().strftime('%Y%m%d%H%M%S')
        shutil.copy2(path, bak)
        changed = False
        # 1) Normalize ext_resource lines: type=... uid=... path=... id=... -> path first
        def repl_ext(m):
            typ = m.group('type')
            pathv = m.group('path')
            idv = m.group('id')
            return f'[ext_resource path="{pathv}" type="{typ}" id={idv}]'
        txt, n1 = re.subn(r'\[ext_resource\s+type="(?P<type>[^"]+)"\s+uid="uid://[^"]+"\s+path="(?P<path>[^"]+)"\s+id=(?P<id>[^\]]+)\]', repl_ext, txt)
        if n1>0:
            changed = True
        # Also handle ext_resource where uid before path with id quoted
        txt, n2 = re.subn(r'\[ext_resource\s+uid="uid://[^"]+"\s+path="(?P<path>[^"]+)"\s+type="(?P<type>[^"]+)"\s+id=(?P<id>[^\]]+)\]', repl_ext, txt)
        if n2>0:
            changed = True
        # 2) Remove uid="uid://..." from gd_scene or ext_resource or gd_resource lines
        txt, n3 = re.subn(r'\s*uid="uid://[^"]+"', '', txt)
        if n3>0:
            changed = True
        # 3) Remove leftover duplicate spaces in bracket starts e.g., '[gd_scene  ' -> single space
        txt = re.sub(r'\[gd_scene\s+', '[gd_scene ', txt)
        # 4) Ensure ext_resource id uses numeric id without quotes if possible
        txt = re.sub(r'\[ext_resource path="(?P<p>[^"]+)" type="(?P<t>[^"]+)" id="(?P<i>\d+)"\]', r'[ext_resource path="\g<p>" type="\g<t>" id=\g<i>]', txt)
        # Write back if changed
        if changed and txt != orig:
            with open(path, 'w', encoding='utf-8', newline='\n') as f:
                f.write(txt)
            modified.append((path, bak))

print('Modified files:')
for m,b in modified:
    print(m)
print('Done.')
