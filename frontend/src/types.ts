export interface Comment {
  id: number;
  authorId: string;
  authorName: string;
  content: string;
  createdAt: string;
  parentId: number | null;
  updatedAt: string | null;
  isDeleted: boolean;
}
