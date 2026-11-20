import { useRef, useState } from 'react';
import { useUploadDocument } from '../documents/useDocuments.ts';
import styles from './UploadButton.module.css';

const MAX_SIZE_BYTES = 10 * 1024 * 1024;

export default function UploadButton({ folderId }: { folderId: number | null }) {
    const upload = useUploadDocument();
    const input = useRef<HTMLInputElement>(null);
    const [error, setError] = useState<string | null>(null);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setError(null);
        upload.reset();

        const file = e.target.files?.[0] ?? null;
        e.target.value = '';
        if (!file) return;

        if (file.size > MAX_SIZE_BYTES) {
            setError('File is larger than 10 MB.');
            return;
        }

        upload.mutate({ file, folderId });
    };

    return (
        <>
            <button
                type="button"
                className={styles.button}
                onClick={() => input.current?.click()}
                disabled={upload.isPending}
            >
                {upload.isPending ? 'Uploading...' : 'Upload PDF'}
            </button>
            <input
                ref={input}
                type="file"
                accept="application/pdf,.pdf"
                hidden
                onChange={handleChange}
            />
            {(error || upload.isError) && (
                <span className={styles.error}>
                    {error ?? upload.error?.message ?? 'Upload failed.'}
                </span>
            )}
            {upload.isSuccess && !error && (
                <span className={styles.success}>Uploaded. Processing...</span>
            )}
        </>
    );
}
