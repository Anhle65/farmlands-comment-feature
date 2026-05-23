import type { Comment } from './types';

// Backend dev server. A later slice moves this to an env var.
const API_BASE_URL = 'http://localhost:8000';

export async function fetchComments(): Promise<Comment[]> {
  const response = await fetch(`${API_BASE_URL}/api/comments`);
  if (!response.ok) {
    throw new Error(`Failed to fetch comments: ${response.status}`);
  }
  return (await response.json()) as Comment[];
}
