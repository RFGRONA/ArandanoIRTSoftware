import os
import re
import subprocess

def strip_space_and_comments(content):
    lines = content.split('\n')
    lines = [l for l in lines if not l.strip().startswith('///')]
    text = ''.join(lines)
    text = re.sub(r'\s+', '', text)
    return text

files_restored = 0
files_skipped = []

out = subprocess.check_output(['git', 'diff', '--name-only', 'd093680', 'HEAD']).decode('utf-8')

for fpath in out.splitlines():
    if not fpath.endswith('.cs'):
        continue
    if not os.path.exists(fpath):
        continue
        
    try:
        old_content = subprocess.check_output(['git', 'show', f'd093680:{fpath}']).decode('utf-8')
    except:
        continue
        
    with open(fpath, 'r', encoding='utf-8') as f:
        curr_content = f.read()
        
    old_stripped = strip_space_and_comments(old_content)
    curr_stripped = strip_space_and_comments(curr_content)
    
    if old_stripped == curr_stripped:
        with open(fpath, 'w', encoding='utf-8') as f:
            f.write(old_content)
        files_restored += 1
    else:
        files_skipped.append(fpath)

print(f"Restored: {files_restored}")
print("Skipped (had actual logic changes):")
for s in files_skipped:
    print(" - " + s)
