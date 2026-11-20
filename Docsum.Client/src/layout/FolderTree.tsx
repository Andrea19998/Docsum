import { useEffect, useMemo, useState } from 'react';
import { Link, useMatch } from 'react-router-dom';
import type { DocumentListItem } from '../documents/types.ts';
import { useCreateFolder, useDeleteFolder, useUpdateFolder } from '../folders/useFolders.ts';
import type { Folder } from '../folders/types.ts';
import styles from './FolderTree.module.css';

type Props = {
    folders: Folder[];
    documents: DocumentListItem[];
    selectedFolderId: number | null;
    onSelectFolder: (id: number | null) => void;
};

const STORAGE_KEY = 'docsum.expandedFolders';

function loadExpanded(): Set<number> {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        const ids = raw ? (JSON.parse(raw) as number[]) : [];
        return new Set(ids);
    } catch {
        return new Set();
    }
}

function sortByName<T extends { name: string }>(items: T[]): T[] {
    return [...items].sort((a, b) =>
        a.name.localeCompare(b.name, 'de', { sensitivity: 'base' }));
}

export default function FolderTree({
    folders,
    documents,
    selectedFolderId,
    onSelectFolder,
}: Props) {
    const [expanded, setExpanded] = useState<Set<number>>(loadExpanded);
    const [menuId, setMenuId] = useState<number | null>(null);
    const [renamingId, setRenamingId] = useState<number | null>(null);
    const [creatingParent, setCreatingParent] = useState<number | null | undefined>(undefined);
    const [actionError, setActionError] = useState<string | null>(null);

    const create = useCreateFolder();
    const update = useUpdateFolder();
    const remove = useDeleteFolder();

    const docMatch = useMatch('/documents/:id');
    const selectedDocId = docMatch ? Number(docMatch.params.id) : null;

    useEffect(() => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify([...expanded]));
    }, [expanded]);

    const byParent = useMemo(() => {
        const map = new Map<number | null, Folder[]>();
        for (const folder of folders) {
            const list = map.get(folder.parentId) ?? [];
            list.push(folder);
            map.set(folder.parentId, list);
        }
        for (const list of map.values()) list.sort((a, b) =>
            a.name.localeCompare(b.name, 'de', { sensitivity: 'base' }));
        return map;
    }, [folders]);

    const docsByFolder = useMemo(() => {
        const map = new Map<number | null, DocumentListItem[]>();
        for (const document of documents) {
            const list = map.get(document.folderId) ?? [];
            list.push(document);
            map.set(document.folderId, list);
        }
        for (const list of map.values()) list.sort((a, b) =>
            a.fileName.localeCompare(b.fileName, 'de', { sensitivity: 'base' }));
        return map;
    }, [documents]);

    const toggle = (id: number) => {
        setExpanded((current) => {
            const next = new Set(current);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const startCreate = (parentId: number | null) => {
        setActionError(null);
        setMenuId(null);
        setRenamingId(null);
        setCreatingParent(parentId);
        if (parentId !== null) {
            setExpanded((current) => new Set(current).add(parentId));
        }
    };

    const submitCreate = (name: string) => {
        const parentId = creatingParent ?? null;
        create.mutate(
            { name, parentId },
            {
                onSuccess: () => setCreatingParent(undefined),
                onError: (error) => setActionError(error.message),
            },
        );
    };

    const submitRename = (folder: Folder, name: string) => {
        update.mutate(
            { id: folder.id, name, parentId: folder.parentId },
            {
                onSuccess: () => setRenamingId(null),
                onError: (error) => setActionError(error.message),
            },
        );
    };

    const confirmDelete = (folder: Folder) => {
        setMenuId(null);
        const ok = window.confirm(
            `Delete folder "${folder.name}"? Its documents and subfolders will move one level up.`,
        );
        if (!ok) return;

        remove.mutate(folder.id, {
            onSuccess: () => {
                if (selectedFolderId === folder.id) onSelectFolder(null);
            },
            onError: (error) => setActionError(error.message),
        });
    };

    const renderDocument = (document: DocumentListItem, depth: number) => (
        <li key={`doc-${document.id}`}>
            <Link
                to={`/documents/${document.id}`}
                className={`${styles.document} ${selectedDocId === document.id ? styles.selected : ''}`}
                style={{ paddingLeft: 12 + depth * 14 }}
                title={document.fileName}
            >
                <span className={styles.docIcon} aria-hidden="true">📄</span>
                <span className={styles.docName}>{document.fileName}</span>
            </Link>
        </li>
    );

    const renderFolder = (folder: Folder, depth: number) => {
        const children = byParent.get(folder.id) ?? [];
        const docs = docsByFolder.get(folder.id) ?? [];
        const isOpen = expanded.has(folder.id);
        const isSelected = selectedFolderId === folder.id;
        const isEmpty = children.length === 0 && docs.length === 0;

        return (
            <li key={`folder-${folder.id}`} className={styles.node}>
                <div className={styles.row} style={{ paddingLeft: 4 + depth * 14 }}>
                    <button
                        type="button"
                        className={styles.chevron}
                        onClick={() => toggle(folder.id)}
                        aria-expanded={isOpen}
                        aria-label={isOpen ? 'Collapse folder' : 'Expand folder'}
                    >
                        {isOpen ? '▾' : '▸'}
                    </button>

                    {renamingId === folder.id ? (
                        <RenameForm
                            initial={folder.name}
                            onSubmit={(name) => submitRename(folder, name)}
                            onCancel={() => setRenamingId(null)}
                        />
                    ) : (
                        <button
                            type="button"
                            className={`${styles.folderName} ${isSelected ? styles.selected : ''}`}
                            onClick={() => {
                                onSelectFolder(folder.id);
                                if (!isOpen) toggle(folder.id);
                            }}
                            title={folder.name}
                        >
                            <span className={styles.folderIcon} aria-hidden="true">
                                {isOpen ? '📂' : '📁'}
                            </span>
                            <span className={styles.nameText}>{folder.name}</span>
                        </button>
                    )}

                    <button
                        type="button"
                        className={styles.menuButton}
                        onClick={() => setMenuId(menuId === folder.id ? null : folder.id)}
                        aria-label={`Actions for ${folder.name}`}
                    >
                        ⋯
                    </button>
                </div>

                {menuId === folder.id && (
                    <div className={styles.actions} style={{ paddingLeft: 24 + depth * 14 }}>
                        <button type="button" onClick={() => startCreate(folder.id)}>New subfolder</button>
                        <button
                            type="button"
                            onClick={() => {
                                setMenuId(null);
                                setRenamingId(folder.id);
                            }}
                        >
                            Rename
                        </button>
                        <button
                            type="button"
                            className={styles.danger}
                            onClick={() => confirmDelete(folder)}
                        >
                            Delete
                        </button>
                    </div>
                )}

                {isOpen && (
                    <ul className={styles.children}>
                        {creatingParent === folder.id && (
                            <li>
                                <NewFolderForm
                                    depth={depth + 1}
                                    onSubmit={submitCreate}
                                    onCancel={() => setCreatingParent(undefined)}
                                />
                            </li>
                        )}
                        {sortByName(children).map((child) => renderFolder(child, depth + 1))}
                        {docs.map((document) => renderDocument(document, depth + 1))}
                        {isEmpty && creatingParent !== folder.id && (
                            <li className={styles.empty} style={{ paddingLeft: 30 + depth * 14 }}>
                                Empty
                            </li>
                        )}
                    </ul>
                )}
            </li>
        );
    };

    const rootFolders = byParent.get(null) ?? [];
    const rootDocs = docsByFolder.get(null) ?? [];

    return (
        <div className={styles.tree}>
            <button type="button" className={styles.newFolder} onClick={() => startCreate(null)}>
                + New folder
            </button>

            {creatingParent === null && (
                <div className={styles.rootCreate}>
                    <NewFolderForm
                        depth={0}
                        onSubmit={submitCreate}
                        onCancel={() => setCreatingParent(undefined)}
                    />
                </div>
            )}

            <ul className={styles.rootList}>
                {sortByName(rootFolders).map((folder) => renderFolder(folder, 0))}
                {rootDocs.map((document) => renderDocument(document, 0))}
            </ul>

            {folders.length === 0 && rootDocs.length === 0 && creatingParent !== null && (
                <p className={styles.hint}>No folders yet. Create one to organise your letters.</p>
            )}

            {actionError && <p className={styles.error}>{actionError}</p>}
        </div>
    );
}

function NewFolderForm({
    depth,
    onSubmit,
    onCancel,
}: {
    depth: number;
    onSubmit: (name: string) => void;
    onCancel: () => void;
}) {
    const [name, setName] = useState('');

    return (
        <form
            className={styles.inlineForm}
            style={{ paddingLeft: 20 + depth * 14 }}
            onSubmit={(e) => {
                e.preventDefault();
                const trimmed = name.trim();
                if (trimmed) onSubmit(trimmed);
            }}
        >
            <input
                autoFocus
                value={name}
                maxLength={100}
                placeholder="Folder name"
                onChange={(e) => setName(e.target.value)}
                onKeyDown={(e) => {
                    if (e.key === 'Escape') onCancel();
                }}
            />
            <button type="submit" disabled={!name.trim()}>Add</button>
        </form>
    );
}

function RenameForm({
    initial,
    onSubmit,
    onCancel,
}: {
    initial: string;
    onSubmit: (name: string) => void;
    onCancel: () => void;
}) {
    const [name, setName] = useState(initial);

    return (
        <form
            className={styles.inlineForm}
            onSubmit={(e) => {
                e.preventDefault();
                const trimmed = name.trim();
                if (trimmed) onSubmit(trimmed);
            }}
        >
            <input
                autoFocus
                value={name}
                maxLength={100}
                onChange={(e) => setName(e.target.value)}
                onKeyDown={(e) => {
                    if (e.key === 'Escape') onCancel();
                }}
            />
            <button type="submit" disabled={!name.trim()}>Save</button>
        </form>
    );
}
