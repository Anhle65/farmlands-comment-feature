import axios from 'axios';
import type { Comment } from './types';

// Backend dev server. A later slice moves this to an env var.
const API_BASE_URL = 'http://localhost:8000';

export interface NewComment{
  authorId: string;
  authorName: string;
  content: string;
  parentId: number | null;
}

export async function fetchComments(): Promise<Comment[]> {
  const response = await axios.get<Comment[]>(`${API_BASE_URL}/api/comments`);
  return response.data;
}

export async function postComment(comment: NewComment): Promise<Comment> {
  const response = await axios.post<Comment>(`${API_BASE_URL}/api/comments`, comment);
  return response.data;
}
