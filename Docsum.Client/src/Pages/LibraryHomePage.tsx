import { Link, useOutletContext } from 'react-router-dom';
import StatusBadge from '../documents/StatusBadge.tsx';
import { useDocuments } from '../documents/useDocuments.ts';
import { useFolders } from '../folders/useFolders.ts';
import { getFolderPath } from '../folders/utils.ts';
import type { LibraryContext } from '../layout/LibraryLayout.tsx';
import styles from './LibraryHomePage.module.css';

function formatDate(iso: string) {
    return new Intl.DateTimeFormat('de-DE', {
        dateStyle: 'medium',
        timeStyle: 'short',
    }).format(new Date(iso));
}

function LibraryHomePage() {
    const { selectedFolderId, setSelectedFolderId } = useOutletContext<LibraryContext>();
    const foldersQuery = useFolders();
    const documentsQuery = useDocuments();

    const folders = foldersQuery.data ?? [];
    const documents = documentsQuery.data ?? [];

    const selectedFolder = folders.find((f) => f.id === selectedFolderId) ?? null;
    const path = selectedFolder ? getFolderPath(folders, selectedFolder.id) : [];

    const subfolders = folders
        .filter((f) => f.parentId === selectedFolderId)
        .sort((a, b) => a.name.localeCompare(b.name, 'de', { sensitivity: 'base' }));

    const docs = documents
        .filter((d) => d.folderId === selectedFolderId)
        .sort((a, b) => b.createdAt.localeCompare(a.createdAt));

    const loading = foldersQuery.isPending || documentsQuery.isPending;
    const error = foldersQuery.isError || documentsQuery.isError;

    return (
        <div className={styles.page}>
            <nav className={styles.breadcrumb} aria-label="Breadcrumb">
                <button
                    type="button"
                    className={styles.crumb}
                    onClick={() => setSelectedFolderId(null)}
                >
                    All documents
                </button>
                {path.map((folder) => (
                    <span key={folder.id} className={styles.crumbGroup}>
                        <span className={styles.separator} aria-hidden="true">/</span>
                        <button
                            type="button"
                            className={`${styles.crumb} ${folder.id === selectedFolderId ? styles.current : ''}`}
                            onClick={() => setSelectedFolderId(folder.id)}
                        >
                            {folder.name}
                        </button>
                    </span>
                ))}
            </nav>

            <header className={styles.heading}>
                <h1 className={styles.title}>{selectedFolder?.name ?? 'All documents'}</h1>
                <p className={styles.meta}>
                    {subfolders.length} {subfolders.length === 1 ? 'folder' : 'folders'}
                    {' · '}
                    {docs.length} {docs.length === 1 ? 'document' : 'documents'}
                </p>
            </header>

            {loading && <p className={styles.hint}>Loading...</p>}

            {error && (
                <p className={styles.error}>
                    {(foldersQuery.error ?? documentsQuery.error) instanceof Error
                        ? (foldersQuery.error ?? documentsQuery.error)!.message
                        : 'Could not load the library.'}
                </p>
            )}

            {!loading && !error && subfolders.length > 0 && (
                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Folders</h2>
                    <div className={styles.folderGrid}>
                        {subfolders.map((folder) => {
                            const documentCount = documents.filter(
                                (d) => d.folderId === folder.id).length;
                            return (
                                <button
                                    key={folder.id}
                                    type="button"
                                    className={styles.folderCard}
                                    onClick={() => setSelectedFolderId(folder.id)}
                                >
                                    <span className={styles.folderIcon} aria-hidden="true">📁</span>
                                    <span className={styles.folderName}>{folder.name}</span>
                                    <span className={styles.folderCount}>
                                        {documentCount} {documentCount === 1 ? 'item' : 'items'}
                                    </span>
                                </button>
                            );
                        })}
                    </div>
                </section>
            )}

            {!loading && !error && (
                <section className={styles.section}>
                    <h2 className={styles.sectionTitle}>Documents</h2>

                    {docs.length === 0 ? (
                        <div className={styles.empty}>
                            <p>No documents here yet.</p>
                            <p className={styles.hint}>
                                Use <strong>Upload PDF</strong> in the sidebar to add a letter
                                {selectedFolder ? ` to ${selectedFolder.name}` : ''}.
                            </p>
                        </div>
                    ) : (
                        <ul className={styles.list}>
                            {docs.map((doc) => (
                                <li key={doc.id}>
                                    <Link to={`/documents/${doc.id}`} className={styles.item}>
                                        <span className={styles.itemIcon} aria-hidden="true">📄</span>
                                        <span className={styles.itemName}>{doc.fileName}</span>
                                        <StatusBadge status={doc.status} />
                                        <span className={styles.itemDate}>{formatDate(doc.createdAt)}</span>
                                    </Link>
                                </li>
                            ))}
                        </ul>
                    )}
                </section>
            )}
        </div>
    );
}

export default LibraryHomePage;
