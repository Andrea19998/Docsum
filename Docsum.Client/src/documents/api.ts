import { api } from '../api/http.ts';
import type {
    ChatExchange,
    ChatMessage,
    DocumentDetail,
    DocumentListItem,
} from './types.ts';

export const listDocuments = () => api<DocumentListItem[]>('/api/documents');

export const getDocument = (id: number) =>
    api<DocumentDetail>(`/api/documents/${id}`);

export const uploadDocument = (file: File, folderId: number | null) => {
    const body = new FormData();
    body.append('file', file); // must match DocumentsController.Upload(IFormFile file)
    if (folderId !== null) body.append('folderId', String(folderId));
    return api<DocumentListItem>('/api/documents', { method: 'POST', body });
};

export const moveDocument = (id: number, folderId: number | null) =>
    api<DocumentListItem>(`/api/documents/${id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ folderId }),
    });

export const deleteDocument = (id: number) =>
    api<void>(`/api/documents/${id}`, { method: 'DELETE' });

export const summarizeDocument = (id: number) =>
    api<DocumentDetail>(`/api/documents/${id}/summarize`, { method: 'POST' });

export const getChatHistory = (id: number) =>
    api<ChatMessage[]>(`/api/documents/${id}/chat`);

export const askDocument = (id: number, question: string) =>
    api<ChatExchange>(`/api/documents/${id}/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ question }),
    });
