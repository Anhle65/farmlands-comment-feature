import React from "react";
import type { Comment } from "../types";

const FIVE_MINUTES_MS = 5 * 60 * 1000;
const MAX_LENGTH = 1000;

interface ICommentCardProps {
    comment: Comment;
    currentUserId: string;
    now: Date;
    onEdit: (id: number, content: string) => Promise<void>;
    onDelete: (id: number) => Promise<void>;
    onReply: (parentId: number, content: string) => Promise<void>;
    isReply?: boolean;
}

const CommentCard = (props: ICommentCardProps) => {
    const { comment, currentUserId, now, onEdit, onDelete, onReply, isReply = false } = props;

    const [isEditing, setIsEditing] = React.useState(false);
    const [editDraft, setEditDraft] = React.useState("");
    const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);
    const [isReplying, setIsReplying] = React.useState(false);
    const [replyDraft, setReplyDraft] = React.useState("");

    const isOwn = comment.authorId === currentUserId;
    const ageMs = now.getTime() - new Date(comment.createdAt).getTime();
    const withinWindow = ageMs < FIVE_MINUTES_MS;
    const canEditOrDelete = isOwn && withinWindow && !comment.isDeleted;

    const handleEditOpen = () => {
        setEditDraft(comment.content);
        setIsEditing(true);
    };

    const handleEditCancel = () => {
        setIsEditing(false);
        setEditDraft("");
    };

    const handleEditSave = async () => {
        await onEdit(comment.id, editDraft.trim());
        setIsEditing(false);
        setEditDraft("");
    };

    const handleDeleteOpen = () => {
        setOpenDeleteDialog(true);
    };

    const handleDeleteCancel = () => {
        setOpenDeleteDialog(false);
    };

    const handleDeleteConfirm = async () => {
        await onDelete(comment.id);
        setOpenDeleteDialog(false);
    };

    const handleReplyOpen = () => {
        setReplyDraft("");
        setIsReplying(true);
    };

    const handleReplyCancel = () => {
        setIsReplying(false);
        setReplyDraft("");
    };

    const handleReplySubmit = async () => {
        await onReply(comment.id, replyDraft.trim());
        setIsReplying(false);
        setReplyDraft("");
    };

    if (comment.isDeleted) {
        return (
            <div className="comment-card comment-tombstone">
                <p>[comment deleted]</p>
            </div>
        );
    }

    const editTrimmed = editDraft.trim();
    const editDisabled = editTrimmed.length === 0 || editDraft.length > MAX_LENGTH;
    const replyTrimmed = replyDraft.trim();
    const replyDisabled = replyTrimmed.length === 0 || replyDraft.length > MAX_LENGTH;

    return (
        <div className={isReply ? "comment-card comment-reply" : "comment-card"}>
            <div className="comment-header">
                <strong>{comment.authorName}</strong>
                <span>
                    {" · "}
                    {new Date(comment.createdAt).toLocaleString()}
                    {comment.updatedAt ? " · (edited)" : ""}
                </span>
            </div>

            {isEditing ? (
                <div className="comment-edit">
                    <textarea
                        autoFocus
                        value={editDraft}
                        onChange={(e) => setEditDraft(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Escape") handleEditCancel();
                        }}
                    />
                    <p className="comment-counter">
                        {editDraft.length} / {MAX_LENGTH}
                    </p>
                    <div className="comment-actions">
                        <button type="button" disabled={editDisabled} onClick={handleEditSave}>
                            Save
                        </button>
                        <button type="button" onClick={handleEditCancel}>
                            Cancel
                        </button>
                    </div>
                </div>
            ) : (
                <p className="comment-content">{comment.content}</p>
            )}

            {!isEditing && !isReplying && (
                <div className="comment-actions">
                    {!isReply && (
                        <button type="button" onClick={handleReplyOpen}>
                            Reply
                        </button>
                    )}
                    {canEditOrDelete && (
                        <>
                            <button type="button" onClick={handleEditOpen}>
                                Edit
                            </button>
                            <button type="button" onClick={handleDeleteOpen}>
                                Delete
                            </button>
                        </>
                    )}
                </div>
            )}

            {openDeleteDialog && (
                <dialog open className="comment-confirm-dialog">
                    <p>Delete this comment?</p>
                    <div className="comment-actions">
                        <button type="button" autoFocus onClick={handleDeleteCancel}>
                            Cancel
                        </button>
                        <button type="button" onClick={handleDeleteConfirm}>
                            Delete
                        </button>
                    </div>
                </dialog>
            )}

            {isReplying && (
                <div className="comment-reply-form">
                    <p className="comment-meta">Replying to {comment.authorName}</p>
                    <textarea
                        autoFocus
                        placeholder="Write a reply…"
                        value={replyDraft}
                        onChange={(e) => setReplyDraft(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Escape") handleReplyCancel();
                        }}
                    />
                    <p className="comment-counter">
                        {replyDraft.length} / {MAX_LENGTH}
                    </p>
                    <div className="comment-actions">
                        <button type="button" disabled={replyDisabled} onClick={handleReplySubmit}>
                            Reply
                        </button>
                        <button type="button" onClick={handleReplyCancel}>
                            Cancel
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default CommentCard;
