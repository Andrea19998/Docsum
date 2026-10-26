import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';

export type CurrentUser = { email: string };

async function fetchMe(): Promise<CurrentUser | null> {
    const res = await fetch('/api/auth/me');
    if (res.status === 401) return null;          // not logged in, not an error
    if (!res.ok) throw new Error('Server error');
    return res.json();
}

export function useAuth() {
    const { data, isLoading, isError } = useQuery({
        queryKey: ['me'],
        queryFn: fetchMe,
        retry: false,
        staleTime: 5 * 60 * 1000,
    });
    return { user: data ?? null, isLoading, isError };
}

export function useLogin() {
    const queryClient = useQueryClient();
    const navigate = useNavigate();

    return useMutation({
        mutationFn: async (data: { email: string; password: string }) => {
            const res = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data),
            });
            if (res.status === 401) throw new Error('Wrong email or password');
            if (!res.ok) throw new Error('Server error, please try again later');
            return res.json();
        },
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ['me'] });
            navigate('/');
        },
    });
}

export function useLogout() {
    const queryClient = useQueryClient();
    const navigate = useNavigate();

    return useMutation({
        mutationFn: () => fetch('/api/auth/logout', { method: 'POST' }),
        onSuccess: () => {
            queryClient.setQueryData(['me'], null);
            navigate('/login');
        },
    });
}