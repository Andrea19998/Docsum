export type Folder = {
    id: number;
    name: string;
    parentId: number | null;
};

export type FolderNode = Folder & { depth: number };
