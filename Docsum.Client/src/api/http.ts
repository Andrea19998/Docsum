export class ApiError extends Error {
    readonly status: number;

    constructor(status: number, message: string) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
    }
}

/**
 * Thin fetch wrapper: throws ApiError with the server's { error } message
 * on non-2xx responses, otherwise returns parsed JSON (or undefined for 204).
 */
export async function api<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(path, init);

    if (!res.ok) {
        let message = `Request failed (${res.status})`;
        try {
            const body = await res.json();
            if (typeof body?.error === 'string') message = body.error;
            else if (typeof body?.title === 'string') message = body.title;
        } catch {
            // response was not JSON; keep the generic message
        }
        throw new ApiError(res.status, message);
    }

    if (res.status === 204) return undefined as T;
    return res.json() as Promise<T>;
}
