import { useEffect, useMemo, useState, type ChangeEvent } from 'react';
import { deleteComment, fetchComments, patchComment, postComment } from '../api';
import type { Comment } from '../types';
import CommentCard from './CommentCard';

type Status = 'loading' | 'ready' | 'error';

const MAX_LENGTH = 1000;

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

  const [isAdding, setIsAdding] = useState(false);
  const [addDraft, setAddDraft] = useState('');

  const handleNameChange = (e: ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value;
    setCurrentUserName(name);
    localStorage.setItem('userName', name);
  };

  const handleEdit = async (id: number, content: string) => {
    const updated = await patchComment(id, content, currentUserId, currentUserName);
    setComments((prev) => prev.map((c) => (c.id === id ? updated : c)));
  };
  const handleDelete = async (id: number) => {
    await deleteComment(id, currentUserId, currentUserName);
    setComments((prev) => prev.filter((c) => c.id !== id));
  };
  const handlePost = async (parentId: number | null, content: string) => {
    const created = await postComment({
      authorId: currentUserId,
      authorName: currentUserName,
      content,
      parentId,
    });
    setComments((prev) => [...prev, created]);
  };

  const handleAddOpen = () => {
    setAddDraft('');
    setIsAdding(true);
  };
  const handleAddCancel = () => {
    setIsAdding(false);
    setAddDraft('');
  };
  const handleAddSubmit = async () => {
    await handlePost(null, addDraft.trim());
    setIsAdding(false);
    setAddDraft('');
  };

  const addTrimmed = addDraft.trim();
  const addDisabled =
    currentUserName.trim().length === 0 ||
    addTrimmed.length === 0 ||
    addDraft.length > MAX_LENGTH;

  return (
    <>
      <div className="comment-list-header">
        <h2>Comments: {status === 'ready' ? `(${comments.length})` : ''}</h2>
        {!isAdding && (
          <button
            type="button"
            onClick={handleAddOpen}
            disabled={currentUserName.trim().length === 0}
            title={
              currentUserName.trim().length === 0
                ? 'Enter your name to comment'
                : undefined
            }
          >
            Add comment
          </button>
        )}
      </div>
      {isAdding && (
        <div className="comment-add-form">
          <textarea
            autoFocus
            placeholder="Write a comment…"
            value={addDraft}
            onChange={(e) => setAddDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Escape') handleAddCancel();
            }}
          />
          <p className="comment-counter">
            {addDraft.length} / {MAX_LENGTH}
          </p>
          <div className="comment-actions">
            <button type="button" disabled={addDisabled} onClick={handleAddSubmit}>
              Submit
            </button>
            <button type="button" onClick={handleAddCancel}>
              Cancel
            </button>
          </div>
        </div>
      )}
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
                onReply={handlePost}
              />
              {(repliesByParent.get(parent.id) ?? []).map((reply) => (
                <CommentCard
                  key={reply.id}
                  comment={reply}
                  currentUserId={currentUserId}
                  now={now}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onReply={handlePost}
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
