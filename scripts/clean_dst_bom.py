from pathlib import Path

dst_dir = Path(r'C:\Users\ulyss\RiderProjects\ArchipelagoP5\ArchipelagoP5RMod\FlowFiles\src')

for dst_file in dst_dir.glob('*.dst'):
    content = dst_file.read_bytes()
    # Strip UTF-8 BOM (\xEF\xBB\xBF)
    if content.startswith(b'\xef\xbb\xbf'):
        content = content[3:]
        dst_file.write_bytes(content)
        print(f'Stripped BOM from {dst_file.name}')
    else:
        print(f'Clean: {dst_file.name}')
