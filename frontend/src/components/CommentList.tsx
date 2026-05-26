import { useEffect, useMemo, useState, type ChangeEvent } from 'react';
import { fetchComments, patchComment, postComment } from '../api';
import type { Comment } from '../types';
import CommentCard from './CommentCard';

type Status = 'loading' | 'ready' | 'error';

export function CommentList() {
  const [comments, setComments] = useState<Comment[]>([]);
  const [status, setStatus] = useState<Status>('loading');

  useEffect(() => {
    fetchComments()
      .then((data) => {
        setComments(data);
        setStatus('ready');
      })
      .catch((error) => {
        console.error(error);
        setStatus('error');
      });
  }, []);

  const { topLevel, repliesByParent } = useMemo(() => {
    const topLevel = comments.filter((c) => c.parentId === null);
    const repliesByParent = new Map<number, Comment[]>();
    for (const c of comments) {
      if (c.parentId === null) continue;
      const bucket = repliesByParent.get(c.parentId) ?? [];
      bucket.push(c);
      repliesByParent.set(c.parentId, bucket);
    }
    for (const bucket of repliesByParent.values()) {
      bucket.sort((a, b) => a.createdAt.localeCompare(b.createdAt));
    }
    return { topLevel, repliesByParent };
  }, [comments]);

  // userId is generated once and persisted; it identifies "this browser" for edit/delete.
  const [currentUserId] = useState(() => {
    let id = localStorage.getItem('userId');
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem('userId', id);
    }
    return id;
  });
  const [currentUserName, setCurrentUserName] = useState(
    () => localStorage.getItem('userName') ?? ''
  );
  const now = new Date();

  const handleNameChange = (e: ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value;
    setCurrentUserName(name);
    localStorage.setItem('userName', name);
  };

  // TODO: wire handleEdit and handleDelete to PATCH / DELETE helpers
  const handleEdit = async (id: number, content: string) => {
    const updated = await patchComment(id, content, currentUserId, currentUserName);
    setComments((prev) => prev.map((c) => (c.id === id ? updated : c)));
  };
  const handleDelete = async (id: number) => {
    console.warn('TODO: DELETE /api/comments/' + id);
  };
  const handleReply = async (parentId: number, content: string) => {
    const created = await postComment({
      authorId: currentUserId,
      authorName: currentUserName,
      content,
      parentId,
    });
    setComments((prev) => [...prev, created]);
  };

  return (
    <>
      <h2>Comments: {status === 'ready' ? `(${comments.length})` : ''}</h2>
      <div className="comment-name-input">
        <label htmlFor="user-name">Your name: </label>
        <input
          id="user-name"
          type="text"
          value={currentUserName}
          onChange={handleNameChange}
          placeholder="Enter your name to comment"
        />
      </div>
      {status === 'loading' && <p className="comment-list">Loading comments…</p>}
      {status === 'error' && (
        <p className="comment-list">Could not load comments. Is the API running?</p>
      )}
      {status === 'ready' && (
        <div className="comment-list">
          {topLevel.map((parent) => (
            <div key={parent.id}>
              <CommentCard
                comment={parent}
                currentUserId={currentUserId}
                now={now}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onReply={handleReply}
              />
              {(repliesByParent.get(parent.id) ?? []).map((reply) => (
                <CommentCard
                  key={reply.id}
                  comment={reply}
                  currentUserId={currentUserId}
                  now={now}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onReply={handleReply}
                  isReply
                />
              ))}
            </div>
          ))}
        </div>
      )}
    </>
  );
}
