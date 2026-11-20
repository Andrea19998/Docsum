import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createFolder, deleteFolder, listFolders, updateFolder } from './api.ts';

const foldersKey = ['folders'] as const;
const documentsKey = ['documents'] as const;

export function useFolders() {
    return useQuery({
        queryKey: foldersKey,
        queryFn: listFolders,
        retry: false,
    });
}

export function useCreateFolder() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ name, parentId }: { name: string; parentId: number | null }) =>
            createFolder(name, parentId),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: foldersKey }),
    });
}

export function useUpdateFolder() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, name, parentId }: { id: number; name: string; parentId: number | null }) =>
            updateFolder(id, name, parentId),
        onSuccess: () => {
            void queryClient.invalidateQueries({ queryKey: foldersKey });
            void queryClient.invalidateQueries({ queryKey: documentsKey });
        },
    });
}

export function useDeleteFolder() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (id: number) => deleteFolder(id),
        onSuccess: () => {
            void queryClient.invalidateQueries({ queryKey: foldersKey });
            void queryClient.invalidateQueries({ queryKey: documentsKey });
        },
    });
}
