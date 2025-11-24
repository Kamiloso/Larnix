import os

class DirNode:
    """Klasa reprezentująca węzeł (katalog) w drzewie wyników."""
    def __init__(self, name, path):
        self.name = name
        self.path = path
        self.local_loc = 0  # Linie kodu tylko w plikach tego katalogu
        self.total_loc = 0  # Linie kodu w tym katalogu + podkatalogach
        self.children = []  # Lista obiektów DirNode (podkatalogi)

def count_lines_in_file(filepath):
    """Zlicza linie w pojedynczym pliku, obsługując błędy kodowania."""
    count = 0
    try:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            for _ in f:
                count += 1
    except Exception as e:
        # Ciche pomijanie błędów odczytu, aby nie śmiecić w konsoli przy dużych projektach
        pass 
    return count

def get_ignored_directories(dir_path):
    """Sprawdza obecność line_abort.txt i zwraca zestaw katalogów do pominięcia."""
    abort_file = os.path.join(dir_path, "line_abort.txt")
    ignored = set()
    
    if os.path.exists(abort_file):
        try:
            with open(abort_file, 'r', encoding='utf-8') as f:
                for line in f:
                    name = line.strip()
                    if name:
                        ignored.add(name)
        except:
            pass     
    return ignored

def scan_directory(current_path):
    """Rekurencyjna funkcja skanująca katalogi i budująca drzewo."""
    folder_name = os.path.basename(current_path)
    if not folder_name:
        folder_name = current_path # Dla katalogu "."
        
    node = DirNode(folder_name, current_path)
    ignored_dirs = get_ignored_directories(current_path)
    
    try:
        items = os.listdir(current_path)
    except PermissionError:
        return node

    files = []
    dirs = []
    
    for item in items:
        full_path = os.path.join(current_path, item)
        if os.path.isfile(full_path):
            files.append(item)
        elif os.path.isdir(full_path):
            dirs.append(item)

    # 1. Zlicz pliki .cs w bieżącym
    for f in files:
        if f.endswith(".cs"):
            full_path = os.path.join(current_path, f)
            node.local_loc += count_lines_in_file(full_path)

    # 2. Rekurencja do podkatalogów
    for d in dirs:
        if d in ignored_dirs:
            continue
            
        full_path = os.path.join(current_path, d)
        child_node = scan_directory(full_path)
        node.children.append(child_node)

    # 3. Sumowanie
    child_sum = sum(child.total_loc for child in node.children)
    node.total_loc = node.local_loc + child_sum
    
    return node

def print_tree(node, prefix="", is_last=True, is_root=True):
    """Wyświetla drzewo, pomijając gałęzie z zerową liczbą linii."""
    
    # Jeśli folder i wszystkie jego podfoldery mają 0 linii kodu - nie wyświetlamy go
    if node.total_loc == 0:
        return

    # Formatowanie liczby (np. 1 234 zamiast 1234)
    loc_display = f"{node.total_loc:,}".replace(",", " ")
    lines_info = f"[{loc_display}]"
    
    if is_root:
        print(f"{node.name}/  {lines_info}")
        new_prefix = ""
    else:
        connector = "└── " if is_last else "├── "
        print(f"{prefix}{connector}{node.name}/  {lines_info}")
        new_prefix = prefix + ("    " if is_last else "│   ")

    # Filtrujemy dzieci - bierzemy tylko te, które mają > 0 linii kodu
    active_children = [c for c in node.children if c.total_loc > 0]
    
    # Sortowanie alfabetyczne
    sorted_children = sorted(active_children, key=lambda x: x.name.lower())
    
    count = len(sorted_children)
    for i, child in enumerate(sorted_children):
        is_last_child = (i == count - 1)
        print_tree(child, new_prefix, is_last_child, is_root=False)

def main():
    start_dir = "."
    # Pobieramy nazwę bieżącego folderu dla ładniejszego nagłówka
    current_folder_name = os.path.basename(os.path.abspath(start_dir))
    
    root_node = scan_directory(start_dir)
    
    print(f"\nRAPORT STRUKTURY KODU (.cs): {current_folder_name}\n")
    print_tree(root_node)

if __name__ == "__main__":
    main()