import { useState } from 'react';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth, useLogout } from '../auth/useAuth.ts';
import { useDocuments } from '../documents/useDocuments.ts';
import { useFolders } from '../folders/useFolders.ts';
import FolderTree from './FolderTree.tsx';
import UploadButton from './UploadButton.tsx';
import styles from './LibraryLayout.module.css';

export type LibraryContext = {
    selectedFolderId: number | null;
    setSelectedFolderId: (id: number | null) => void;
};

function LibraryLayout() {
    const { user } = useAuth();
    const logout = useLogout();
    const navigate = useNavigate();

    const [selectedFolderId, setSelectedFolderId] = useState<number | null>(null);
    const [sidebarOpen, setSidebarOpen] = useState(false);

    const foldersQuery = useFolders();
    const documentsQuery = useDocuments();
    const folders = foldersQuery.data ?? [];
    const documents = documentsQuery.data ?? [];

    // A folder may disappear after a delete (or in another tab); derive the safe value in render.
    const activeFolderId = selectedFolderId !== null
        && folders.some((f) => f.id === selectedFolderId)
        ? selectedFolderId
        : null;
    const selectedFolder = folders.find((f) => f.id === activeFolderId) ?? null;

    const selectFolder = (id: number | null) => {
        setSelectedFolderId(id);
        setSidebarOpen(false);
        navigate('/');
    };

    return (
        <div className={styles.shell}>
            <header className={styles.header}>
                <button
                    type="button"
                    className={styles.menuToggle}
                    onClick={() => setSidebarOpen((open) => !open)}
                    aria-label="Toggle folders"
                    aria-expanded={sidebarOpen}
                >
                    ☰
                </button>
                <Link to="/" className={styles.brand}>Docsum</Link>
                <div className={styles.account}>
                    <span className={styles.email}>{user?.email}</span>
                    <button className={styles.logout} onClick={() => logout.mutate()}>
                        Log out
                    </button>
                </div>
            </header>

            <div className={styles.body}>
                <aside className={`${styles.sidebar} ${sidebarOpen ? styles.open : ''}`}>
                    <div className={styles.uploadBox}>
                        <span className={styles.uploadTarget}>
                            Upload to <strong>{selectedFolder?.name ?? 'All documents'}</strong>
                        </span>
                        <UploadButton folderId={activeFolderId} />
                    </div>

                    <nav className={styles.nav} aria-label="Document library">
                        <button
                            type="button"
                            className={`${styles.allRow} ${activeFolderId === null ? styles.selected : ''}`}
                            onClick={() => selectFolder(null)}
                        >
                            <span aria-hidden="true">🗂️</span>
                            <span className={styles.allLabel}>All documents</span>
                            <span className={styles.count}>{documents.length}</span>
                        </button>

                        {foldersQuery.isPending && <p className={styles.hint}>Loading folders...</p>}
                        {foldersQuery.isError && (
                            <p className={styles.error}>
                                {foldersQuery.error instanceof Error
                                    ? foldersQuery.error.message
                                    : 'Could not load folders.'}
                            </p>
                        )}

                        <FolderTree
                            folders={folders}
                            documents={documents}
                            selectedFolderId={activeFolderId}
                            onSelectFolder={selectFolder}
                        />
                    </nav>
                </aside>

                {sidebarOpen && (
                    <button
                        type="button"
                        className={styles.overlay}
                        aria-label="Close folders"
                        onClick={() => setSidebarOpen(false)}
                    />
                )}

                <section className={styles.content}>
                    <Outlet context={{ selectedFolderId: activeFolderId, setSelectedFolderId } satisfies LibraryContext} />
                </section>
            </div>
        </div>
    );
}

export default LibraryLayout;
