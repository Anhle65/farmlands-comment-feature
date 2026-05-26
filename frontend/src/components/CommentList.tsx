import { useEffect, useMemo, useState, type ChangeEvent } from 'react';
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material';
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
    const topLevel = comments
      .filter((c) => c.parentId === null)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
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
  const [addName, setAddName] = useState('');
  const [addDraft, setAddDraft] = useState('');
  const [addError, setAddError] = useState('');
  const [addErrorFlag, setAddErrorFlag] = useState(false);

  const handleEdit = async (id: number, content: string) => {
    const updated = await patchComment(id, content, currentUserId, currentUserName);
    setComments((prev) => prev.map((c) => (c.id === id ? updated : c)));
  };
  const handleDelete = async (id: number) => {
    await deleteComment(id, currentUserId, currentUserName);
    setComments((prev) =>
      prev.map((c) => (c.id === id ? { ...c, isDeleted: true } : c)),
    );
  };
  const handlePost = async (parentId: number | null, name: string, content: string) => {
    setCurrentUserName(name);
    localStorage.setItem('userName', name);
    const created = await postComment({
      authorId: currentUserId,
      authorName: name,
      content,
      parentId,
    });
    setComments((prev) => [...prev, created]);
  };

  const updateAddNameState = (event: ChangeEvent<HTMLInputElement>) => {
    setAddName(event.target.value);
    setAddError('');
    setAddErrorFlag(false);
  };
  const updateAddContentState = (event: ChangeEvent<HTMLTextAreaElement>) => {
    setAddDraft(event.target.value);
    setAddError('');
    setAddErrorFlag(false);
  };

  const handleAddOpen = () => {
    setAddName(currentUserName);
    setAddDraft('');
    setAddError('');
    setAddErrorFlag(false);
    setIsAdding(true);
  };
  const handleAddCancel = () => {
    setIsAdding(false);
    setAddName('');
    setAddDraft('');
    setAddError('');
    setAddErrorFlag(false);
  };
  const onAddSubmit = async () => {
    setAddError('');
    setAddErrorFlag(false);
    if (addName.trim().length === 0) {
      setAddError('Name can not be empty');
      setAddErrorFlag(true);
      return;
    }
    if (addDraft.trim().length === 0) {
      setAddError('Comment can not be empty');
      setAddErrorFlag(true);
      return;
    }
    if (addDraft.length > MAX_LENGTH) {
      setAddError(`Comment must be under ${MAX_LENGTH} characters`);
      setAddErrorFlag(true);
      return;
    }
    try {
      await handlePost(null, addName.trim(), addDraft.trim());
      setIsAdding(false);
      setAddName('');
      setAddDraft('');
    } catch (error) {
      console.error(error);
      setAddError('Failed to post comment');
      setAddErrorFlag(true);
    }
  };

  return (
    <>
      <Stack
        direction="row"
        sx={{ px: 4, mt: 3, alignItems: 'center', justifyContent: 'space-between' }}
      >
        <Typography variant="h5" component="h2">
          Comments: {status === 'ready' ? `(${comments.filter((c) => !c.isDeleted).length})` : ''}
        </Typography>
        {!isAdding && (
          <Button variant="contained" onClick={handleAddOpen}>
            Add comment
          </Button>
        )}
      </Stack>
      {isAdding && (
        <Box
          component="form"
          sx={{
            mx: 4,
            my: 2,
            p: 2,
            textAlign: 'left',
            border: 1,
            borderColor: 'divider',
            borderRadius: 1,
          }}
        >
          <Stack direction="column" spacing={2}>
            {addErrorFlag && <Alert severity="error">{addError}</Alert>}
            <TextField
              fullWidth
              required
              autoFocus
              id="add-name"
              label="Your name"
              value={addName}
              onChange={updateAddNameState}
              onKeyDown={(e) => {
                if (e.key === 'Escape') handleAddCancel();
              }}
            />
            <TextField
              fullWidth
              required
              multiline
              rows={3}
              id="add-content"
              label="Comment"
              placeholder="Write a comment…"
              value={addDraft}
              onChange={updateAddContentState}
              onKeyDown={(e) => {
                if (e.key === 'Escape') handleAddCancel();
              }}
              slotProps={{ htmlInput: { maxLength: MAX_LENGTH } }}
            />
            <Typography variant="caption" sx={{ textAlign: 'right' }}>
              {addDraft.length} / {MAX_LENGTH}
            </Typography>
            <Stack direction="row" spacing={1}>
              <Button
                variant="contained"
                onClick={(e) => {
                  e.preventDefault();
                  onAddSubmit();
                }}
              >
                Submit
              </Button>
              <Button variant="outlined" onClick={handleAddCancel}>
                Cancel
              </Button>
            </Stack>
          </Stack>
        </Box>
      )}
      {status === 'loading' && <p className="comment-list">Loading comments…</p>}
      {status === 'error' && (
        <p className="comment-list">Could not load comments. Is the API running?</p>
      )}
      {status === 'ready' && (
        <div className="comment-list">
          {topLevel.map((parent) => {
            const visibleReplies = (repliesByParent.get(parent.id) ?? []).filter(
              (r) => !r.isDeleted,
            );
            if (parent.isDeleted && visibleReplies.length === 0) return null;
            return (
            <div key={parent.id}>
              <CommentCard
                comment={parent}
                currentUserId={currentUserId}
                currentUserName={currentUserName}
                now={now}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onReply={handlePost}
              />
              {visibleReplies.map((reply) => (
                <CommentCard
                  key={reply.id}
                  comment={reply}
                  currentUserId={currentUserId}
                  currentUserName={currentUserName}
                  now={now}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onReply={handlePost}
                  isReply
                  parentAuthorName={parent.isDeleted ? null : parent.authorName}
                />
              ))}
            </div>
            );
          })}
        </div>
      )}
    </>
  );
}
