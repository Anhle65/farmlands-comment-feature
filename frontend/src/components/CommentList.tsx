import { useEffect, useMemo, useState } from 'react';
import { fetchComments } from '../api';
import type { Comment } from '../types';

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

  if (status === 'loading') return <p>Loading comments…</p>;
  if (status === 'error') return <p>Could not load comments. Is the API running?</p>;

  return (
    <div className="comment-list">
      <p>{comments.length} comments</p>
      {topLevel.map((parent) => (
        <div key={parent.id}>
          <CommentRow comment={parent} />
          {(repliesByParent.get(parent.id) ?? []).map((reply) => (
            <CommentRow key={reply.id} comment={reply} isReply />
          ))}
        </div>
      ))}
    </div>
  );
}

function CommentRow({ comment, isReply = false }: { comment: Comment; isReply?: boolean }) {
  return (
    <div className={isReply ? 'comment-reply' : undefined}>
      <p>
        {isReply ? '↳ ' : ''}
        <strong>{comment.authorName}</strong>
        {' · '}
        {new Date(comment.createdAt).toLocaleString()}
        {comment.updatedAt ? ' · (edited)' : ''}
        {comment.parentId !== null ? ` · reply to #${comment.parentId}` : ''}
      </p>
      <p>{comment.isDeleted ? '[deleted]' : comment.content}</p>
    </div>
  );
}
