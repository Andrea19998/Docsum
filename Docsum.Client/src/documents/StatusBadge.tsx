import type { DocumentStatus } from './types.ts';
import styles from './StatusBadge.module.css';

const labels: Record<DocumentStatus, string> = {
    Uploaded: 'Uploaded',
    Processing: 'Processing',
    Done: 'Done',
    Failed: 'Failed',
};

export default function StatusBadge({ status }: { status: DocumentStatus }) {
    return (
        <span className={`${styles.badge} ${styles[status.toLowerCase()]}`}>
            {labels[status]}
        </span>
    );
}
