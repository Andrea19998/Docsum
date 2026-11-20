import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
    askDocument,
    deleteDocument,
    getChatHistory,
    getDocument,
    listDocuments,
    moveDocument,
    uploadDocument,
} from './api.ts';

const documentsKey = ['documents'] as const;

export function useDocuments() {
    return useQuery({
        queryKey: documentsKey,
        queryFn: listDocuments,
        retry: false,
        // Keep the sidebar in sync while documents are being processed.
        refetchInterval: (query) => {
            const docs = query.state.data;
            if (!docs) return false;
            return docs.some((d) => d.status === 'Uploaded' || d.status === 'Processing')
                ? 2000
                : false;
        },
    });
}

export function useDocument(id: number) {
    return useQuery({
        queryKey: ['documents', id],
        queryFn: () => getDocument(id),
        enabled: Number.isInteger(id) && id > 0,
        retry: false,
        refetchInterval: (query) => {
            const status = query.state.data?.status;
            return status === 'Uploaded' || status === 'Processing' ? 2000 : false;
        },
    });
}

export function useUploadDocument() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ file, folderId }: { file: File; folderId: number | null }) =>
            uploadDocument(file, folderId),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: documentsKey }),
    });
}

export function useMoveDocument() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, folderId }: { id: number; folderId: number | null }) =>
            moveDocument(id, folderId),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: documentsKey }),
    });
}

export function useDeleteDocument() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (id: number) => deleteDocument(id),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: documentsKey }),
    });
}

export function useChatHistory(id: number) {
    return useQuery({
        queryKey: ['documents', id, 'chat'],
        queryFn: () => getChatHistory(id),
        enabled: Number.isInteger(id) && id > 0,
        retry: false,
    });
}

export function useAskDocument(id: number) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (question: string) => askDocument(id, question),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['documents', id, 'chat'] }),
    });
}
