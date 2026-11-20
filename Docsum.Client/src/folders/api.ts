import { api } from '../api/http.ts';
import type { Folder } from './types.ts';

export const listFolders = () => api<Folder[]>('/api/folders');

export const createFolder = (name: string, parentId: number | null) =>
    api<Folder>('/api/folders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, parentId }),
    });

export const updateFolder = (id: number, name: string, parentId: number | null) =>
    api<Folder>(`/api/folders/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, parentId }),
    });

export const deleteFolder = (id: number) =>
    api<void>(`/api/folders/${id}`, { method: 'DELETE' });
