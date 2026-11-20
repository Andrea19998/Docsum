export type DocumentStatus = 'Uploaded' | 'Processing' | 'Done' | 'Failed';

export type DocumentListItem = {
    id: number;
    fileName: string;
    folderId: number | null;
    status: DocumentStatus;
    createdAt: string;
};

export type DocumentDetail = DocumentListItem & {
    summary: string | null;
    errorMessage: string | null;
};

export type ChatRole = 'user' | 'assistant';

export type ChatMessage = {
    id: number;
    role: ChatRole;
    content: string;
    createdAt: string;
};

export type ChatExchange = {
    user: ChatMessage;
    assistant: ChatMessage;
};
