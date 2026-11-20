import type { Folder, FolderNode } from './types.ts';

/** Root-to-folder chain, used for breadcrumbs. */
export function getFolderPath(folders: Folder[], id: number): Folder[] {
    const byId = new Map(folders.map((f) => [f.id, f]));
    const path: Folder[] = [];
    let current = byId.get(id);

    while (current) {
        path.unshift(current);
        current = current.parentId === null ? undefined : byId.get(current.parentId);
    }

    return path;
}

export function flattenFolders(
    folders: Folder[],
    parentId: number | null = null,
    depth = 0,
): FolderNode[] {
    const result: FolderNode[] = [];

    for (const folder of folders
        .filter((f) => f.parentId === parentId)
        .sort((a, b) => a.name.localeCompare(b.name, 'de', { sensitivity: 'base' }))) {
        result.push({ ...folder, depth });
        result.push(...flattenFolders(folders, folder.id, depth + 1));
    }

    return result;
}
