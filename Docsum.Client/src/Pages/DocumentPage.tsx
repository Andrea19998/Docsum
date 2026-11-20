import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import StatusBadge from '../documents/StatusBadge.tsx';
import {
    useAskDocument,
    useChatHistory,
    useDeleteDocument,
    useDocument,
    useMoveDocument,
} from '../documents/useDocuments.ts';
import { useFolders } from '../folders/useFolders.ts';
import { flattenFolders } from '../folders/utils.ts';
import styles from './DocumentPage.module.css';

function formatDate(iso: string) {
    return new Intl.DateTimeFormat('de-DE', {
        dateStyle: 'long',
        timeStyle: 'short',
    }).format(new Date(iso));
}

function DocumentPage() {
    const id = Number(useParams().id);
    const navigate = useNavigate();

    const document = useDocument(id);
    const foldersQuery = useFolders();
    const move = useMoveDocument();
    const remove = useDeleteDocument();
    const history = useChatHistory(id);
    const ask = useAskDocument(id);

    const [question, setQuestion] = useState('');

    const doc = document.data;
    const canChat = doc?.status === 'Done';

    const send = (text: string) => {
        const trimmed = text.trim();
        if (!trimmed || ask.isPending || !canChat) return;

        ask.mutate(trimmed, {
            onSuccess: () => setQuestion(''),
        });
    };

    const handleDelete = () => {
        if (!doc) return;
        if (!window.confirm(`Delete "${doc.fileName}"? This cannot be undone.`)) return;

        remove.mutate(doc.id, { onSuccess: () => navigate('/') });
    };

    if (!Number.isInteger(id) || id <= 0) {
        return (
            <div className={styles.page}>
                <p className={styles.error}>Invalid document id.</p>
            </div>
        );
    }

    return (
        <div className={styles.page}>
            <nav className={styles.breadcrumb} aria-label="Breadcrumb">
                <Link to="/" className={styles.crumb}>Library</Link>
                {doc && (
                    <>
                        <span className={styles.separator} aria-hidden="true">/</span>
                        <span className={styles.current}>{doc.fileName}</span>
                    </>
                )}
            </nav>

            {document.isPending && <p className={styles.hint}>Loading...</p>}

            {document.isError && (
                <p className={styles.error}>
                    {document.error instanceof Error
                        ? document.error.message
                        : 'Could not load document.'}
                </p>
            )}

            {doc && (
                <>
                    <header className={styles.heading}>
                        <div className={styles.titleRow}>
                            <h1 className={styles.title}>{doc.fileName}</h1>
                            <StatusBadge status={doc.status} />
                        </div>
                        <p className={styles.meta}>Uploaded {formatDate(doc.createdAt)}</p>
                    </header>

                    <div className={styles.toolbar}>
                        <label className={styles.moveLabel}>
                            Folder
                            <select
                                className={styles.select}
                                value={doc.folderId ?? ''}
                                disabled={move.isPending}
                                onChange={(e) => move.mutate({
                                    id: doc.id,
                                    folderId: e.target.value === '' ? null : Number(e.target.value),
                                })}
                            >
                                <option value="">All documents (root)</option>
                                {flattenFolders(foldersQuery.data ?? []).map((folder) => (
                                    <option key={folder.id} value={folder.id}>
                                        {'\u00A0\u00A0'.repeat(folder.depth)}
                                        {folder.depth > 0 ? '↳ ' : ''}
                                        {folder.name}
                                    </option>
                                ))}
                            </select>
                        </label>

                        <button
                            type="button"
                            className={styles.deleteButton}
                            onClick={handleDelete}
                            disabled={remove.isPending}
                        >
                            {remove.isPending ? 'Deleting...' : 'Delete'}
                        </button>
                    </div>

                    {(move.isError || remove.isError) && (
                        <p className={styles.error}>
                            {(move.error ?? remove.error)?.message}
                        </p>
                    )}

                    {doc.status === 'Failed' && doc.errorMessage && (
                        <p className={styles.error}>{doc.errorMessage}</p>
                    )}

                    <section className={`${styles.card} ${styles.summaryCard}`}>
                        <h2 className={styles.cardTitle}>Summary</h2>
                        {doc.status === 'Uploaded' || doc.status === 'Processing' ? (
                            <p className={styles.hint}>
                                Processing this letter. The summary will appear here shortly.
                            </p>
                        ) : doc.summary ? (
                            <p className={styles.summary}>{doc.summary}</p>
                        ) : (
                            <p className={styles.hint}>
                                No summary available. Add an AI provider key to generate summaries.
                            </p>
                        )}
                    </section>

                    <section className={`${styles.card} ${styles.chatCard}`}>
                        <h2 className={styles.cardTitle}>Ask about this letter</h2>

                        <div className={styles.chatLog}>
                            {history.isPending && <p className={styles.hint}>Loading conversation...</p>}

                            {history.isError && (
                                <p className={styles.error}>
                                    {history.error instanceof Error
                                        ? history.error.message
                                        : 'Could not load the conversation.'}
                                </p>
                            )}

                            {history.data?.length === 0 && !history.isPending && (
                                <div className={styles.emptyChat}>
                                    <p className={styles.hint}>
                                        Ask a question and the answer will be grounded in this letter.
                                    </p>
                                    {canChat && (
                                        <div className={styles.suggestions}>
                                            {['What do I need to do?', 'Is there a deadline?', 'How much do I have to pay?'].map((suggestion) => (
                                                <button
                                                    key={suggestion}
                                                    type="button"
                                                    className={styles.suggestion}
                                                    disabled={ask.isPending}
                                                    onClick={() => send(suggestion)}
                                                >
                                                    {suggestion}
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            )}

                            {history.data?.map((message) => (
                                <div
                                    key={message.id}
                                    className={message.role === 'user' ? styles.userMessage : styles.assistantMessage}
                                >
                                    {message.content}
                                </div>
                            ))}

                            {ask.isPending && (
                                <div className={styles.assistantMessage}>
                                    <span className={styles.thinking}>Thinking...</span>
                                </div>
                            )}
                        </div>

                        {ask.isError && <p className={styles.error}>{ask.error.message}</p>}

                        <form
                            className={styles.composer}
                            onSubmit={(e) => {
                                e.preventDefault();
                                send(question);
                            }}
                        >
                            <textarea
                                value={question}
                                rows={2}
                                maxLength={2000}
                                placeholder={canChat
                                    ? 'Ask about deadlines, amounts, or what to do next...'
                                    : 'Waiting for the document to be processed...'}
                                disabled={!canChat || ask.isPending}
                                onChange={(e) => setQuestion(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter' && !e.shiftKey) {
                                        e.preventDefault();
                                        send(question);
                                    }
                                }}
                            />
                            <button
                                type="submit"
                                className={styles.sendButton}
                                disabled={!canChat || ask.isPending || !question.trim()}
                            >
                                {ask.isPending ? 'Sending...' : 'Send'}
                            </button>
                        </form>
                        <p className={styles.disclaimer}>
                            Answers are generated from the letter text and are not legal advice.
                        </p>
                    </section>
                </>
            )}
        </div>
    );
}

export default DocumentPage;
