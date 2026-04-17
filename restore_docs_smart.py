import subprocess
import os

out = subprocess.check_output(['git', 'diff', '--name-only', 'd093680', 'HEAD']).decode('utf-8')
csharp_files = [line.strip() for line in out.splitlines() if line.strip().endswith('.cs')]

def extract_docs(old_content):
    docs = []
    lines = old_content.split('\n')
    current_doc = []
    current_attributes = []
    
    for i, line in enumerate(lines):
        stripped = line.strip()
        if stripped.startswith('///'):
            current_doc.append(line)
        elif stripped.startswith('['):
            if current_doc:
                current_attributes.append(line)
        elif current_doc:
            if stripped and not stripped.startswith('//'):
                docs.append((current_doc, stripped, current_attributes))
                current_doc = []
                current_attributes = []
    return docs

restored_count = 0
for fpath in csharp_files:
    if not os.path.exists(fpath): continue
    try:
        old_content = subprocess.check_output(['git', 'show', f'd093680:{fpath}']).decode('utf-8')
    except:
        continue
        
    docs = extract_docs(old_content)
    if not docs: continue
    
    with open(fpath, 'r', encoding='utf-8') as f:
        curr_lines = f.read().split('\n')
        
    output_lines = []
    inserted_sigs = set()
    insertions = {}
    
    for docblock, sig, attrs in docs:
        for i, line in enumerate(curr_lines):
            if line.strip() == sig and sig not in inserted_sigs:
                insert_idx = i
                while insert_idx > 0 and curr_lines[insert_idx-1].strip().startswith('['):
                    insert_idx -= 1
                    
                if insert_idx > 0 and curr_lines[insert_idx-1].strip().startswith('///'):
                    pass # Already has docs
                else:
                    insertions[insert_idx] = docblock
                inserted_sigs.add(sig)
                break
                
    if not insertions:
        continue
        
    for i, line in enumerate(curr_lines):
        if i in insertions:
            for d in insertions[i]:
                output_lines.append(d)
        output_lines.append(line)
        
    with open(fpath, 'w', encoding='utf-8') as f:
        f.write('\n'.join(output_lines))
    restored_count += 1
    print(f"Restored {len(insertions)} doc blocks in {fpath}")

print(f"Total files patched: {restored_count}")
