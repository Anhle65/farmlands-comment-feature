import React from "react";
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Stack,
    TextField,
    Typography,
} from "@mui/material";
import type { Comment } from "../types";

const FIVE_MINUTES_MS = 5 * 60 * 1000;
const MAX_LENGTH = 1000;

interface ICommentCardProps {
    comment: Comment;
    currentUserId: string;
    currentUserName: string;
    now: Date;
    onEdit: (id: number, content: string) => Promise<void>;
    onDelete: (id: number) => Promise<void>;
    onReply: (parentId: number, name: string, content: string) => Promise<void>;
    isReply?: boolean;
    parentAuthorName?: string | null;
}

const CommentCard = (props: ICommentCardProps) => {
    const { comment, currentUserId, currentUserName, now, onEdit, onDelete, onReply, isReply = false, parentAuthorName } = props;

    const [isEditing, setIsEditing] = React.useState(false);
    const [editDraft, setEditDraft] = React.useState("");
    const [editError, setEditError] = React.useState("");
    const [editErrorFlag, setEditErrorFlag] = React.useState(false);
    const [openDeleteDialog, setOpenDeleteDialog] = React.useState(false);
    const [deleteError, setDeleteError] = React.useState("");
    const [deleteErrorFlag, setDeleteErrorFlag] = React.useState(false);
    const [isReplying, setIsReplying] = React.useState(false);
    const [replyName, setReplyName] = React.useState("");
    const [replyDraft, setReplyDraft] = React.useState("");
    const [replyError, setReplyError] = React.useState("");
    const [replyErrorFlag, setReplyErrorFlag] = React.useState(false);

    const isOwn = comment.authorId === currentUserId && comment.authorName === currentUserName;
    const ageMs = now.getTime() - new Date(comment.createdAt).getTime();
    const withinWindow = ageMs < FIVE_MINUTES_MS;
    const canEditOrDelete = isOwn && withinWindow && !comment.isDeleted;

    const updateEditContentState = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        setEditDraft(event.target.value);
        setEditError("");
        setEditErrorFlag(false);
    };

    const handleEditOpen = () => {
        setEditDraft(comment.content);
        setEditError("");
        setEditErrorFlag(false);
        setIsEditing(true);
    };

    const handleEditCancel = () => {
        setIsEditing(false);
        setEditDraft("");
        setEditError("");
        setEditErrorFlag(false);
    };

    const handleEditSave = async () => {
        setEditError("");
        setEditErrorFlag(false);
        const ageNow = Date.now() - new Date(comment.createdAt).getTime();
        if (ageNow >= FIVE_MINUTES_MS) {
            setEditError("You can no longer edit this comment (past 5-minute window)");
            setEditErrorFlag(true);
            return;
        }
        try {
            await onEdit(comment.id, editDraft.trim());
            setIsEditing(false);
            setEditDraft("");
        } catch (error: unknown) {
            const status =
                typeof error === "object" && error !== null && "response" in error
                    ? (error as { response?: { status?: number } }).response?.status
                    : undefined;
            if (status === 403) {
                setEditError("You can no longer edit this comment (past 5-minute window)");
            } else {
                setEditError("Failed to save comment");
            }
            setEditErrorFlag(true);
        }
    };

    const handleDeleteOpen = () => {
        setDeleteError("");
        setDeleteErrorFlag(false);
        setOpenDeleteDialog(true);
    };

    const handleDeleteCancel = () => {
        setOpenDeleteDialog(false);
        setDeleteError("");
        setDeleteErrorFlag(false);
    };

    const handleDeleteConfirm = async () => {
        setDeleteError("");
        setDeleteErrorFlag(false);
        try {
            await onDelete(comment.id);
            setOpenDeleteDialog(false);
        } catch (error: unknown) {
            const status =
                typeof error === "object" && error !== null && "response" in error
                    ? (error as { response?: { status?: number } }).response?.status
                    : undefined;
            if (status === 403) {
                setDeleteError("You can no longer delete this comment (past 5-minute window)");
            } else {
                setDeleteError("Failed to delete comment");
            }
            setDeleteErrorFlag(true);
        }
    };

    const updateReplyNameState = (event: React.ChangeEvent<HTMLInputElement>) => {
        setReplyName(event.target.value);
        setReplyError("");
        setReplyErrorFlag(false);
    };

    const updateReplyContentState = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
        setReplyDraft(event.target.value);
        setReplyError("");
        setReplyErrorFlag(false);
    };

    const handleReplyOpen = () => {
        setReplyName(currentUserName);
        setReplyDraft("");
        setReplyError("");
        setReplyErrorFlag(false);
        setIsReplying(true);
    };

    const handleReplyCancel = () => {
        setIsReplying(false);
        setReplyName("");
        setReplyDraft("");
        setReplyError("");
        setReplyErrorFlag(false);
    };

    const onReplySubmit = async () => {
        setReplyError("");
        setReplyErrorFlag(false);
        if (replyName.trim().length === 0) {
            setReplyError("Name can not be empty");
            setReplyErrorFlag(true);
            return;
        }
        if (replyDraft.trim().length === 0) {
            setReplyError("Reply can not be empty");
            setReplyErrorFlag(true);
            return;
        }
        if (replyDraft.length > MAX_LENGTH) {
            setReplyError(`Reply must be under ${MAX_LENGTH} characters`);
            setReplyErrorFlag(true);
            return;
        }
        try {
            await onReply(comment.id, replyName.trim(), replyDraft.trim());
            setIsReplying(false);
            setReplyName("");
            setReplyDraft("");
        } catch (error) {
            console.error(error);
            setReplyError("Failed to post reply");
            setReplyErrorFlag(true);
        }
    };

    const cardSx = {
        mb: 2,
        ml: isReply ? 4 : 0,
        textAlign: "left" as const,
        border: "none",
        boxShadow: "none",
    };

    if (comment.isDeleted) {
        return (
            <Card sx={{ ...cardSx, opacity: 0.6 }}>
                <CardContent sx={{ py: 1.5, "&:last-child": { pb: 1.5 } }}>
                    <Typography variant="body2" color="text.secondary">
                        [comment deleted]
                    </Typography>
                </CardContent>
            </Card>
        );
    }

    const editTrimmed = editDraft.trim();
    const editDisabled = editTrimmed.length === 0 || editDraft.length > MAX_LENGTH;

    const contentLabel = isReply
        ? parentAuthorName
            ? `Replied to ${parentAuthorName}`
            : "Replied to deleted comment"
        : undefined;

    const card = (
        <Card sx={{ ...cardSx, ml: 0, flex: 1 }}>
            <CardContent sx={{ py: 1.5, "&:last-child": { pb: 1.5 } }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: "baseline" }}>
                    <Typography
                        variant="caption"
                        component="div"
                        sx={{ fontWeight: 600, textDecoration: "underline" }}
                    >
                        {comment.authorName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                        · {new Date(comment.createdAt).toLocaleString()}
                        {comment.updatedAt ? " · (edited)" : ""}
                    </Typography>
                </Stack>

                {isEditing ? (
                    <Stack direction="column" spacing={2} sx={{ mt: 2 }}>
                        {editErrorFlag && <Alert severity="error">{editError}</Alert>}
                        <TextField
                            fullWidth
                            required
                            multiline
                            rows={3}
                            autoFocus
                            label="Edit comment"
                            value={editDraft}
                            onChange={updateEditContentState}
                            onKeyDown={(e) => {
                                if (e.key === "Escape") handleEditCancel();
                            }}
                            slotProps={{ htmlInput: { maxLength: MAX_LENGTH } }}
                        />
                        <Typography variant="caption" sx={{ textAlign: "right" }}>
                            {editDraft.length} / {MAX_LENGTH}
                        </Typography>
                        <Stack direction="row" spacing={1}>
                            <Button variant="contained" disabled={editDisabled} onClick={handleEditSave}>
                                Save
                            </Button>
                            <Button variant="outlined" onClick={handleEditCancel}>
                                Cancel
                            </Button>
                        </Stack>
                    </Stack>
                ) : (
                    <TextField
                        fullWidth
                        multiline
                        minRows={1}
                        // maxRows={6}
                        variant="standard"
                        label={contentLabel}
                        value={comment.content}
                        slotProps={{ input: { readOnly: true } }}
                        sx={{
                            mt: 0.5,
                            mb: 1,
                            "& .MuiInput-underline:before": { borderBottom: "none" },
                            "& .MuiInput-underline:after": { borderBottom: "none" },
                            "& .MuiInput-underline:hover:not(.Mui-disabled):before": {
                                borderBottom: "none",
                            },
                        }}
                    />
                )}

                {!isEditing && !isReplying && (
                    <Stack direction="row" spacing={1}>
                        {!isReply && (
                            <Button
                                size="small"
                                onClick={handleReplyOpen}
                                sx={{
                                    fontSize: "0.7rem",
                                    py: 0.25,
                                    px: 1,
                                    minWidth: 0,
                                    textTransform: "none",
                                }}
                            >
                                Reply
                            </Button>
                        )}
                        {canEditOrDelete && (
                            <>
                                <Button size="small" onClick={handleEditOpen}>
                                    Edit
                                </Button>
                                <Button size="small" color="error" onClick={handleDeleteOpen}>
                                    Delete
                                </Button>
                            </>
                        )}
                    </Stack>
                )}

                <Dialog
                    open={openDeleteDialog}
                    onClose={handleDeleteCancel}
                    aria-labelledby="delete-dialog-title"
                    aria-describedby="delete-dialog-description"
                >
                    <DialogTitle id="delete-dialog-title">Delete this comment?</DialogTitle>
                    <DialogContent>
                        {deleteErrorFlag && (
                            <Alert severity="error" sx={{ mb: 2 }}>
                                {deleteError}
                            </Alert>
                        )}
                        <DialogContentText id="delete-dialog-description">
                            Do you want to delete this comment? This can not be undone.
                        </DialogContentText>
                    </DialogContent>
                    <DialogActions>
                        <Button autoFocus onClick={handleDeleteCancel}>
                            Cancel
                        </Button>
                        <Button variant="outlined" color="error" onClick={handleDeleteConfirm}>
                            Delete
                        </Button>
                    </DialogActions>
                </Dialog>

            {isReplying && (
                <Box
                    component="form"
                    sx={{
                        mt: 1,
                        p: 2,
                        border: 1,
                        borderColor: "divider",
                        borderRadius: 1,
                        textAlign: "left",
                    }}
                >
                    <Stack direction="column" spacing={2}>
                        <Typography variant="body2" color="text.secondary">
                            Replying to {comment.authorName}
                        </Typography>
                        {replyErrorFlag && <Alert severity="error">{replyError}</Alert>}
                        <TextField
                            fullWidth
                            required
                            autoFocus
                            id={`reply-name-${comment.id}`}
                            label="Your name"
                            value={replyName}
                            onChange={updateReplyNameState}
                            onKeyDown={(e) => {
                                if (e.key === "Escape") handleReplyCancel();
                            }}
                        />
                        <TextField
                            fullWidth
                            required
                            multiline
                            rows={3}
                            id={`reply-content-${comment.id}`}
                            label="Reply"
                            placeholder="Write a reply…"
                            value={replyDraft}
                            onChange={updateReplyContentState}
                            onKeyDown={(e) => {
                                if (e.key === "Escape") handleReplyCancel();
                            }}
                            slotProps={{ htmlInput: { maxLength: MAX_LENGTH } }}
                        />
                        <Typography variant="caption" sx={{ textAlign: "right" }}>
                            {replyDraft.length} / {MAX_LENGTH}
                        </Typography>
                        <Stack direction="row" spacing={1}>
                            <Button
                                variant="contained"
                                onClick={(e) => {
                                    e.preventDefault();
                                    onReplySubmit();
                                }}
                            >
                                Reply
                            </Button>
                            <Button variant="outlined" onClick={handleReplyCancel}>
                                Cancel
                            </Button>
                        </Stack>
                    </Stack>
                </Box>
            )}
            </CardContent>
        </Card>
    );

    if (isReply) {
        return (
            <Box sx={{ display: "flex", alignItems: "flex-start", ml: 2 }}>
                <Typography
                    component="span"
                    color="text.secondary"
                    aria-hidden
                    sx={{ pt: 1.5, pr: 1, fontSize: "1.1rem", lineHeight: 1 }}
                >
                    ↳
                </Typography>
                {card}
            </Box>
        );
    }

    return card;
};

export default CommentCard;
