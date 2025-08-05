import { useCallback, useEffect, useState } from "react";
import { $queryClient } from "../api/api";
import { toast } from "react-toastify";
import type { FileMetadata } from "../api/fileApi";
import type { ChatMessage } from "../api/apiTypes";
import { useDialogContext } from "./useDialogContext";


export function useChatOperations(chatId: string | undefined, serverId?: string) {
    const dialogs = useDialogContext();
    const [attachment, setAttachment] = useState<FileMetadata | null>(null);
    const [message, setMessage] = useState<string>("");
    const [role, setRole] = useState<"user" | "assistant" | "system" | "tool">("user");
    const [messageToEdit, setMessageToEdit] = useState<ChatMessage | undefined>(undefined);
    
    // Get chat data
    const { 
        data: chatData, 
        error, 
        isFetching, 
        isLoading, 
        refetch, 
        failureReason 
    } = $queryClient.useQuery(
        "get", 
        "/api/AppChat/{id}", 
        { params: { path: { id: chatId! } } }, 
        {
            refetchOnWindowFocus: false, 
            retryOnMount: false, 
            refetchOnMount: false, 
            refetchOnReconnect: false, 
            enabled: !!chatId 
        }
    );


    const [selectedToolIds, setSelectedToolIds] = useState<string[]>([]);
    useEffect(() => {
        setSelectedToolIds(chatData?.options?.enabledToolIds ?? []);
    }, [chatData]);

    const mutateChatError = useCallback((error: never) => {
        toast.error("Error sending message: " + error);
        // No need to manipulate messages since we're not doing optimistic updates
    }, []);

    // Add message mutation
    const { mutate: mutateChatAddAsync, isPending: isPendingAdd } = $queryClient.useMutation(
        "put", 
        "/api/AppChat/{id}", 
        {
            onMutate: async (variables) => {
                // Optimistically add message immediately
                if (chatData?.messages) {
                    const maxIndex = chatData.messages.length > 0 
                        ? Math.max(...chatData.messages.map(m => m.index || 0))
                        : -1;
                    
                    const tempMessage = {
                        // id: crypto.randomUUID(),
                        index: maxIndex + 1,
                        role: variables.body.role,
                        content: variables.body.message,
                        createdAt: new Date().toISOString(),
                        images: variables.body.images || [],
                        enabledToolIds: variables.body.enabledToolIds || []
                    };
                    
                    // Add to current messages optimistically
                    chatData.messages.push(tempMessage);
                }
                
                // Clear form immediately for better UX
                setMessage("");
                setAttachment(null);
            },
            onSuccess: () => {
                // Refetch to get the correct server state
                refetch();
            },
            onError: (e) => {
                // On error, refetch to get correct state and restore form
                refetch();
                mutateChatError(e);
            }
        }
    );

    // Delete message mutation
    const { mutate: mutateChatDeleteAsync, isPending: isPendingDelete } = $queryClient.useMutation(
        "delete", 
        "/api/AppChat/{id}/{messageId}", 
        {
            onSuccess: () => {
                // Just refetch to get the correct server state
                refetch();
            },
            onError: e => mutateChatError(e)
        }
    );

    // Edit message mutation
    const { mutate: mutateChatEditAsync, isPending: isPendingEdit } = $queryClient.useMutation(
        "put", 
        "/api/AppChat/{id}/{messageId}", 
        {
            onSuccess: () => {
                // Just refetch to get the correct server state
                refetch();
            },
            onError: e => mutateChatError(e)
        }
    );

    // Send message mutation
    const { mutate: mutateChatSendAsync, isPending: isPendingSend } = $queryClient.useMutation(
        "post", 
        "/api/AppChat/{id}", 
        {
            onMutate: async (variables) => {
                // Optimistically add user message immediately
                if (chatData?.messages) {
                    const maxIndex = chatData.messages.length > 0 
                        ? Math.max(...chatData.messages.map(m => m.index || 0))
                        : -1;
                    
                    const tempMessage = {
                        // id: crypto.randomUUID(),
                        index: maxIndex + 1,
                        role: variables.body.role,
                        content: variables.body.message,
                        createdAt: new Date().toISOString(),
                        images: variables.body.images || [],
                        enabledToolIds: variables.body.enabledToolIds || []
                    };
                    
                    // Add to current messages optimistically
                    chatData.messages.push(tempMessage);
                }
                
                // Clear form immediately for better UX
                setMessage("");
                setAttachment(null);
            },
            onSuccess: () => {
                // Refetch to get the actual server response and AI reply
                refetch();
            },
            onError: (e) => {
                // On error, refetch to get correct state and restore form
                refetch();
                mutateChatError(e);
            }
        }
    );

    // Resubmit chat mutation
    const { mutate: mutateChatResubmitAsync, isPending: isPendingResubmit } = $queryClient.useMutation(
        "post", 
        "/api/AppChat/{id}/resubmit", 
        {
            onSuccess: () => {
                // Refetch the chat data to get the updated state
                refetch();
            },
            onError: e => mutateChatError(e)
        }
    );

    // Status flags
    const isBusy = isLoading || isPendingAdd || isPendingSend || isFetching || isPendingDelete || isPendingEdit || isPendingResubmit;
    const awaitingReply = isLoading || isFetching || isPendingSend || isPendingAdd || isPendingResubmit;

    // Helper functions
    const sendMessage = useCallback(() => {
        if (!chatId || isBusy || !message) return;

        const actualServerId = serverId || chatData?.chatServerId;
        if (!actualServerId) {
            toast.error('No server available for chat operation');
            return;
        }
        
        mutateChatSendAsync({ 
            params: { path: { id: chatId } }, 
            body: { 
                message, 
                role, 
                enabledToolIds: selectedToolIds, 
                images: attachment ? [attachment.path] : []
            } 
        });
    }, [chatId, serverId, chatData, message, role, attachment, selectedToolIds, mutateChatSendAsync, isBusy]);

    const addMessage = useCallback(() => {
        if (!chatId || isBusy || !message) return;

        const actualServerId = serverId || chatData?.chatServerId;
        if (!actualServerId) {
            toast.error('No server available for chat operation');
            return;
        }
        
        mutateChatAddAsync({ 
            params: { path: { id: chatId } }, 
            body: { 
                message, 
                role, 
                enabledToolIds: selectedToolIds, 
                images: attachment ? [attachment.path] : [] 
            } 
        });
    }, [chatId, serverId, chatData, isBusy, message, mutateChatAddAsync, role, selectedToolIds, attachment]);

    const resubmitChat = useCallback(() => {
        if (!chatId || isBusy) return;
        
        const actualServerId = serverId || chatData?.chatServerId;
        if (!actualServerId) {
            toast.error('No server available for chat operation');
            return;
        }
        
        mutateChatResubmitAsync({ params: { path: { id: chatId } } });
    }, [chatId, serverId, chatData, mutateChatResubmitAsync, isBusy]);

    const deleteMessage = useCallback((id: string) => {
        if (!chatId || isBusy) return;
        
        // Find the message to get context for the confirmation
        const message = chatData?.messages?.find(m => m.id === id);
        const messagePreview = message?.content?.substring(0, 50) + (message?.content && message.content.length > 50 ? "..." : "");

        dialogs.showDangerConfirmDialog(
            "Delete Message",
            `Are you sure you want to delete this message${messagePreview ? `:\n"${messagePreview}"` : ""}?\n\nThis action cannot be undone.`,
            () => {
                mutateChatDeleteAsync({ params: { path: { id: chatId, messageId: id } } });
            }, undefined,
        );
    }, [chatId, mutateChatDeleteAsync, isBusy, chatData?.messages, dialogs]);

    const editMessage = useCallback((messageId: string) => {
        const message = chatData?.messages?.find(m => m.id === messageId);
        setMessageToEdit(message);
        return message;
    }, [chatData]);

    const saveEditedMessage = useCallback(() => {
        if (!chatId || !messageToEdit) return;
        
        mutateChatEditAsync({
            params: { path: { id: chatId, messageId: messageToEdit.id! } },
            body: {
                message: messageToEdit.content ?? "",
                role: messageToEdit.role,
                images: messageToEdit.images ?? [],
            }
        });
    }, [chatId, messageToEdit, mutateChatEditAsync]);

    return {
        chatData,
        error,
        failureReason,
        isFetching,
        isLoading,
        refetch,
        message,
        setMessage,
        attachment,
        setAttachment,
        role,
        setRole,
        messageToEdit,
        setMessageToEdit,
        isBusy,
        isPendingAdd,
        isPendingSend,
        isPendingResubmit,
        awaitingReply,
        sendMessage,
        addMessage,
        resubmitChat,
        deleteMessage,
        editMessage,
        saveEditedMessage,
        setSelectedToolIds,
        selectedToolIds,
    };
}
