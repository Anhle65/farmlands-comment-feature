import { useEffect, useState } from 'react';
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

  if (status === 'loading') return <p>Loading comments…</p>;
  if (status === 'error') return <p>Could not load comments. Is the API running?</p>;

  return (
    <div>
      <p>{comments.length} comments</p>
      {comments.map((comment) => (
        <div key={comment.id}>
          <p>
            <strong>{comment.authorName}</strong>
            {' · '}
            {new Date(comment.createdAt).toLocaleString()}
            {comment.updatedAt ? ' · (edited)' : ''}
            {comment.parentId !== null ? ` · reply to #${comment.parentId}` : ''}
          </p>
          <p>{comment.isDeleted ? '[deleted]' : comment.content}</p>
        </div>
      ))}
    </div>
  );
}
