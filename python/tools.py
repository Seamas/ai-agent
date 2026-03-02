
import os
from pathlib import Path

base_dir = Path("./test")

def read_file(name: str) -> str:
    print(f"(read_file: {name})")
    try: 
        with open(base_dir / name, "r") as f:
            content: str = f.read()
            
        return content
    except Exception as e:
        return f"An error occured: {e}"


def list_files() -> list[str]:
    print(f"(list_files)")
    file_list = []
    for item in base_dir.rglob("*"):
        if item.is_file():
            file_list.append(str(item.relative_to(base_dir)))
    return file_list


def rename_file(name: str, new_name: str) -> str:
    print(f"(rename_file: {name} -> {new_name})")
    try:
        new_path = base_dir / new_name
        if not str(new_path).startswith(str(base_dir)):
            return  "Error: new_name is outside base_dir."
        
        os.makedirs(new_path.parent, exist_ok=True)
        os.rename(base_dir / name, new_path)
        return f"File '{name}' successfully rename to '{new_name}'"
    except Exception as e:
        return f"An error occurred: {e}"