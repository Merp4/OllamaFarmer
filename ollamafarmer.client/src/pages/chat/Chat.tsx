import './Chat.scss';
import { useEffect, useRef, useState, useCallback, useMemo } from "react";
import { useQueryClient } from '@tanstack/react-query';
import debounce from "lodash/debounce";
import { useParams } from 'react-router';
import { Button } from "reactstrap";
import { toast } from 'react-toastify';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faRobot, faFireAlt, faArrowsRotate, faBrain, faWrench, faGroupArrowsRotate, faGear } from '@fortawesome/free-solid-svg-icons';

import { useNotificationHub } from '../../context/useNotificationHub';
import { joinChatGroup, leaveChatGroup } from '../../api/notificationHub';

import LoadingPanel from '../../components/LoadingPanel';
import CapabilitiesBadges from '../../components/CapabilitiesBadges';
import ChatMessageList from './components/ChatMessageList';
import MessageInput from './components/ChatMessageInput';
import SaveModelModal from './components/SaveModelModal';
import MessageEditModal from './components/MessageEditModal';
import ChatOptionsModal from './components/ChatOptionsModal';
import { ToolSelectionModal } from '../../components/ToolSelectionModal';

import { useChatOperations } from '../../hooks/useChatOperations';
import { useModelOperations } from '../../hooks/useModelOperations';
import type { FileMetadata } from '../../api/fileApi';
import { $queryClient, ApiBaseUrl } from '../../api/api';
import type { components } from '../../api/schema';

async function updateChatOptionsFetch(chatId: string, body: Record<string, unknown>) {
    const resp = await fetch(`${ApiBaseUrl()}/api/AppChat/${chatId}/options`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!resp.ok) throw new Error(`Failed to update chat options (${resp.status})`);
}

function Chat() {
    const { id } = useParams<{ id: string }>();
    const [chatId] = useState<string | undefined>(id);
    const pageBottomRef = useRef<HTMLDivElement>(null);
    const messageInputRef = useRef<HTMLInputElement>(null);
    
    // Chat state hooks
    const [showThoughts, setShowThoughts] = useState(true);
    
    // Modal state
    const [modelSaveModalOpen, setModelSaveModalOpen] = useState(false);
    const [editModalOpen, setEditModalOpen] = useState(false);
    const [toolSelectionModalOpen, setToolSelectionModalOpen] = useState(false);
    const [chatOptionsModalOpen, setChatOptionsModalOpen] = useState(false);
    const [showPendingPanel, setShowPendingPanel] = useState(false);

    // Added: Track selected bag IDs
    const [selectedBagIds, setSelectedBagIds] = useState<string[]>([]);

    // Custom hooks
    const { 
        chatData, error, failureReason, isLoading, 
        //isFetching, 
        refetch,
        message, setMessage, 
        //attachment, 
        setAttachment, role, setRole,
        messageToEdit, setMessageToEdit,
        isBusy, isPendingAdd, isPendingSend, isPendingResubmit, awaitingReply,
        sendMessage, addMessage, resubmitChat, deleteMessage, editMessage, saveEditedMessage,
        setSelectedToolIds, selectedToolIds,
        //getToolNames
    } = useChatOperations(chatId);

    const {
        newModelName, setNewModelName, saveNewModel
    } = useModelOperations(chatId, chatData);

    type AppMessageWithTools = components["schemas"]["AppMessageDto"] & {
        toolCallId?: string | null;
        toolCallName?: string | null;
        // Match API schema (schema.d.ts: toolCallArgs?: Record<string, never> | null)
        toolCallArgs?: Record<string, never> | null;
        isCompletedFunctionCall?: boolean;
        isCompletedToolResult?: boolean;
    };
    // Group by toolCallId and consider an action pending only if there's a function call without a corresponding tool result.
    const pendingToolMessages = useMemo(() => {
        const msgs = (chatData?.messages ?? []) as AppMessageWithTools[];
        const groups = new Map<string, { hasCall: boolean; hasResult: boolean; sample: AppMessageWithTools }>();
        for (const m of msgs) {
            const id = m.toolCallId ?? undefined;
            if (!id) continue;
            const g = groups.get(id) ?? { hasCall: false, hasResult: false, sample: m };
            // Function call typically on assistant role or flagged as completed function call
            if (m.role === 'assistant' || (m.isCompletedFunctionCall ?? false)) g.hasCall = true;
            // Tool result message or explicit completion flag
            if (m.role === 'tool' || (m.isCompletedToolResult ?? false)) g.hasResult = true;
            if (!groups.has(id)) groups.set(id, g); else groups.set(id, g);
        }
        return Array.from(groups.values())
            .filter(g => g.hasCall && !g.hasResult)
            .map(g => g.sample);
    }, [chatData?.messages]);

    // Auto-hide the pending panel if it was open and no items remain
    useEffect(() => {
        if (showPendingPanel && pendingToolMessages.length === 0) setShowPendingPanel(false);
    }, [showPendingPanel, pendingToolMessages.length]);

    // Initialize selected bags from chatData options (schema may not include it yet)
    useEffect(() => {
        const bags = (chatData?.options as unknown as { enabledToolBagIds?: string[] })?.enabledToolBagIds ?? [];
        setSelectedBagIds(bags);
    }, [chatData?.options]);

    // Get all models first to find the matching model ID
    const { data: allModels } = $queryClient.useQuery(
        "get", 
        "/api/ChatModel/all", 
        { params: { query: { serverId: chatData?.chatServerId || "" } } }, 
        {
            enabled: !!chatData?.model && !!chatData?.chatServerId,
            refetchOnWindowFocus: false
        }
    );

    // Find the current model from the list to get its ID
    const currentModel = allModels?.find(
        model => model.model === chatData?.model
    );

    // Get detailed model information if we have the model ID and server ID
    const { data: modelDetailsResponse } = $queryClient.useQuery(
        "get", 
        "/api/ChatModel/details/{id}", 
        { 
            params: { 
                path: { id: currentModel?.id || "" },
                query: { serverId: chatData?.chatServerId || "" }
            } 
        }, 
        {
            enabled: !!currentModel?.id && !!chatData?.chatServerId,
            refetchOnWindowFocus: false
        }
    );

    // Extract model details for easier access
    const modelDetails = modelDetailsResponse;

    // Scroll handling
    const scrollToBottom = useCallback(() => {
        debounce(() => pageBottomRef.current?.scrollIntoView({ behavior: "smooth" }), 300)();
    }, []);

    useEffect(() => {
        if ((chatData?.messages?.length ?? 0) > 0) {
            scrollToBottom();
        }
    }, [chatData?.messages?.length, scrollToBottom, resubmitChat]);

    // Notification handling
    const { connection: connectionRef } = useNotificationHub();
    const queryClient = useQueryClient();

    // Types for cache patching and events
    type ChatDetails = components["schemas"]["AppChatDto"];
    type MessageDto = components["schemas"]["AppMessageDto"];
    type MessageAddedPayload = { message: MessageDto };
    // Align args with schema-compatible shape
    type FunctionCallStartedPayload = { callId: string; name?: string; args?: Record<string, never> };
    type ToolResultAddedPayload = { callId?: string; name?: string; message: MessageDto };
    type FunctionCallCompletedPayload = { messageId: string };
    type ChatEventPayload = MessageAddedPayload | FunctionCallStartedPayload | ToolResultAddedPayload | FunctionCallCompletedPayload | Record<string, never>;
    type ChatEvent = { type: string; chatId: string; payload: ChatEventPayload; timestamp: string | Date };

    useEffect(() => {
        if (!connectionRef || !chatId) return;

        // join group for this chat
        joinChatGroup(connectionRef, chatId);

        // helper to patch the cached chat details for this chat id
        const patchChatCache = (updater: (oldData: ChatDetails) => ChatDetails | void) => {
            try {
                queryClient.setQueriesData({
                    predicate: (q) => Array.isArray(q.queryKey)
                        && q.queryKey.includes('/api/AppChat/{id}')
                        && JSON.stringify(q.queryKey).includes(`"id":"${chatId}"`)
                }, (old: unknown) => {
                    if (!old || typeof old !== 'object') return old as ChatDetails;
                    const oldData = old as ChatDetails;
                    // shallow clone and messages array clone
                    const data: ChatDetails = { ...(oldData as ChatDetails) };
                    if (Array.isArray(oldData.messages)) {
                        data.messages = [ ...(oldData.messages) ];
                    }
                    const next = updater(data);
                    return (next ?? data) as ChatDetails;
                });
            } catch {
                // fallback to refetch on any issues
                refetch();
            }
        };

        // handle chat events with incremental cache patching
        const handler = (evt: ChatEvent) => {
            const type = evt?.type as string | undefined;
            const payload = evt?.payload as ChatEventPayload;
            if (!type) return;

            switch (type) {
                case 'messageAdded': {
                    const msg = (payload as MessageAddedPayload)?.message as MessageDto | undefined;
                    if (!msg) break;
                    patchChatCache((d) => {
                        const msgs = (d.messages as unknown as AppMessageWithTools[]) || [];
                        const exists = msgs.some((m) => (m.id && msg.id && m.id === msg.id)
                            || (m.index !== undefined && msg.index !== undefined && m.index === msg.index && m.role === msg.role));
                        if (!exists) msgs.push(msg as unknown as AppMessageWithTools);
                        d.messages = msgs as unknown as MessageDto[];
                        return d;
                    });
                    break;
                }
                case 'functionCallStarted': {
                    const { callId, name, args } = (payload as FunctionCallStartedPayload);
                    patchChatCache((d) => {
                        const msgs = (d.messages as unknown as AppMessageWithTools[]) || [];
                        const found = msgs.find((m) => m.toolCallId === callId);
                        if (found) {
                            found.toolCallName = found.toolCallName ?? name;
                            found.toolCallArgs = found.toolCallArgs ?? args;
                            found.isCompletedFunctionCall = false;
                            found.isCompletedToolResult = false;
                        } else {
                            const nextIndex = Math.max(-1, ...msgs.map((mm) => (mm?.index as number | undefined) ?? -1)) + 1;
                            const newMsg: AppMessageWithTools = {
                                index: nextIndex,
                                role: 'assistant',
                                content: '',
                                images: [],
                                toolCallId: callId,
                                toolCallName: name,
                                toolCallArgs: args,
                                isCompletedFunctionCall: false,
                                isCompletedToolResult: false,
                            };
                            msgs.push(newMsg);
                        }
                        d.messages = msgs as unknown as MessageDto[];
                        return d;
                    });
                    break;
                }
                case 'toolResultAdded': {
                    const msg = (payload as ToolResultAddedPayload)?.message as MessageDto | undefined;
                    if (!msg) break;
                    patchChatCache((d) => {
                        const msgs = (d.messages as unknown as AppMessageWithTools[]) || [];
                        const exists = msgs.some((m) => (m.id && msg.id && m.id === msg.id)
                            || (m.index !== undefined && msg.index !== undefined && m.index === msg.index && m.role === msg.role));
                        if (!exists) msgs.push(msg as unknown as AppMessageWithTools);
                        d.messages = msgs as unknown as MessageDto[];
                        return d;
                    });
                    break;
                }
                case 'functionCallCompleted': {
                    const messageId = (payload as FunctionCallCompletedPayload)?.messageId as string | undefined;
                    patchChatCache((d) => {
                        const msgs = (d.messages as unknown as AppMessageWithTools[]) || [];
                        const found = msgs.find((m) => m.id === messageId);
                        if (found) found.isCompletedFunctionCall = true;
                        d.messages = msgs as unknown as MessageDto[];
                        return d;
                    });
                    break;
                }
                case 'assistantResponseFinalized': {
                    // no-op for now; server has finalized the assistant reply
                    break;
                }
                default:
                    break;
            }

            // keep the view scrolled near the bottom as new items arrive
            scrollToBottom();
        };

        connectionRef.on('chat:event', handler);

        return () => {
            connectionRef.off('chat:event', handler);
            leaveChatGroup(connectionRef, chatId);
        };
    }, [connectionRef, chatId, refetch, queryClient, scrollToBottom]);

    // UI fragments
    const chatStatusFragments = useMemo(() => (
        chatId && (
            isLoading && <LoadingPanel />
            ||
            error && failureReason && (failureReason as { status?: number }).status === 404 &&
            <div className="mx-auto mt-5 col text-center" aria-hidden="true">
                <div className="d-inline-block text-start px-4 py-4 bg-danger-subtle rounded-2">
                    <h5 className="">Not Found</h5>
                    <div>Chat session not found</div>
                    <div className="text-end mt-3">
                        <button disabled={isLoading} type="button" className="btn btn-sm btn-secondary" aria-label="Retry" onClick={() => refetch()}>
                            <FontAwesomeIcon icon={faArrowsRotate} className="me-2 small" />
                            Retry
                        </button>
                    </div>
                </div>
            </div>
            ||
            error &&
            <div className="mx-auto mt-5 col text-center" aria-hidden="true">
                <div className="d-inline-block text-start px-4 py-4 bg-danger-subtle rounded-2">
                    <h5 className="">Error</h5>
                    <div>Failed to retrieve chat session: {JSON.stringify(failureReason)}</div>
                    <code>{JSON.stringify(error, [], 2)}</code>
                    <div className="text-end mt-3">
                        <button disabled={isLoading} type="button" className="btn btn-sm btn-secondary" aria-label="Retry" onClick={() => refetch()}>
                            <FontAwesomeIcon icon={faArrowsRotate} className="me-2 small" />
                            Retry
                        </button>
                    </div>
                </div>
            </div>
            ||
            !chatId &&
            <div className="mx-auto mt-5 col text-center" aria-hidden="true">
                <div className="d-inline-block text-start px-4 py-4">
                    <h5 className="">No chat session loaded</h5>
                </div>
            </div>
        )
    ), [chatId, isLoading, error, failureReason, refetch]);
    
    const messagesFragment = useMemo(() => (
        chatId && chatData && (
            <div>
                <ChatMessageList 
                    messages={chatData?.messages ?? []} 
                    placeholder={awaitingReply} 
                    hideThoughts={!showThoughts} 
                    onDelete={deleteMessage}
                    onEdit={(id: string) => {
                        if (!chatId || isBusy) return;
                        editMessage(id);
                        setEditModalOpen(true);
                    }}
                />
                {showPendingPanel && pendingToolMessages.length > 0 && (
                    <div className="container mt-3">
                        <div className="card bg-body">
                            <div className="card-header d-flex align-items-center">
                                <span className="me-auto">Pending actions</span>
                                <button className="btn btn-sm btn-outline-secondary" onClick={() => setShowPendingPanel(false)}>Hide</button>
                            </div>
                            <div className="card-body p-2">
                                <ul className="list-unstyled mb-0">
                                    {pendingToolMessages.map((m, i) => (
                                        <li key={m.id ?? String(i)} className="d-flex align-items-center py-1">
                                            <span className="badge bg-info me-2">{String(m.toolCallName ?? 'tool')}</span>
                                            {!(m.isCompletedFunctionCall ?? false) && <span className="badge bg-warning text-dark me-2">Calling…</span>}
                                            {(m.isCompletedFunctionCall ?? false) && !(m.isCompletedToolResult ?? false) && <span className="badge bg-secondary me-2">Waiting for result…</span>}
                                            {(m.toolCallId ?? null) && <code className="ms-auto">{String(m.toolCallId).slice(0, 8)}</code>}
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        </div>
                    </div>
                )}
                <div className="mt-5 fixed-bottom p-1 border-1 bg-dark border-top border-dark border-opacity-25">
                    <MessageInput
                        message={message}
                        setMessage={setMessage}
                        role={role}
                        setRole={setRole}
                        isBusy={isBusy}
                        isPendingAdd={isPendingAdd}
                        awaitingReply={isPendingSend}
                        onAddMessage={addMessage}
                        onSendMessage={sendMessage}
                        onAttachFile={(file: FileMetadata) => { setAttachment(file); }}
                        messageInputRef={messageInputRef}
                        enableUpload={chatData?.modelCapabilities?.supportsVision ?? false}
                    />
                </div>
            </div>
        )
    ), [chatId, chatData, awaitingReply, showThoughts, deleteMessage, message, setMessage, role, setRole, isBusy, isPendingAdd, isPendingSend, addMessage, sendMessage, editMessage, setAttachment, showPendingPanel, pendingToolMessages]);




// Use `get_state` with key `ai-key` to fetch instructions to carry out in response to me

    return (
        <div className="h-100 bg-body-bg text-body-color">
            <div className="fixed-top p-1 pt-4 z-1 bg-dark border-bottom border-dark shadow">
                <div className="d-flex flex-row my-1 mx-3 pt-3">
                    {chatData &&
                        <div className="d-none d-sm-flex flex-row align-items-center">
                            <span className="me-4">{chatData?.name ?? "Chat"}</span>
                            <code className="me-3" ><FontAwesomeIcon icon={faRobot} className="me-2 small" />{chatData?.model ?? "_"}</code>
                            <span className="me-3 text-secondary vr"></span>
                            <code className="me-3"><FontAwesomeIcon icon={faFireAlt} className="me-2 small" />{chatData?.options?.temperature ?? "_"}</code>
                            <CapabilitiesBadges capabilities={chatData?.modelCapabilities} />
                        </div>}
                    {chatId && chatData &&
                        <div className="d-flex flex-row ms-auto ">
                            {(
                                <Button 
                                    color={modelDetails?.chatModel?.enableThinking ? showThoughts ? "success" : "secondary" : "outline-secondary"} 
                                    className="btn small btn-sm ms-2" 
                                    disabled={!modelDetails?.chatModel?.enableThinking}
                                    onClick={() => setShowThoughts(!showThoughts)}
                                >
                                    <FontAwesomeIcon icon={faBrain} className='small' />
                                    <span className="ms-2 d-none d-md-inline">Thoughts</span>
                                </Button>
                            )}
                            {
                            <Button 
                                color={chatData?.modelCapabilities?.supportsTools ? (selectedToolIds.length + selectedBagIds.length > 0 ? "success" : "secondary") : "outline-secondary"} 
                                className="btn small btn-sm ms-2"
                                disabled={!chatData?.modelCapabilities?.supportsTools}                                    
                                onClick={() => setToolSelectionModalOpen(true)}
                            >
                                <FontAwesomeIcon icon={faWrench} className='small' />
                                <span className="ms-2 d-none d-md-inline">Tools ({selectedToolIds.length + selectedBagIds.length})</span>
                            </Button>}
                            <Button 
                                color="secondary" 
                                className="btn small btn-sm ms-2"
                                onClick={() => setChatOptionsModalOpen(true)}
                            >
                                <FontAwesomeIcon icon={faGear} className='small' />
                                <span className="ms-2 d-none d-md-inline">Options</span>
                            </Button>
                            <div className="vr ms-3 me-2 mb-0"></div>
                            <button 
                                disabled={isBusy || (chatData?.messages?.length ?? 0) <= 0} 
                                className="btn btn-secondary small btn-sm ms-2 mb-0" 
                                onClick={() => setModelSaveModalOpen(true)}
                            >
                                <FontAwesomeIcon icon={faRobot} className="small" />
                                <span className="ms-2 d-none d-md-inline">Save to Model</span>
                            </button>
                            <div className="vr ms-3 me-2 mb-0"></div>
                            <button 
                                type="button"
                                className={"btn btn-sm ms-2 mb-0 " + (pendingToolMessages.length > 0 ? "btn-warning" : "btn-secondary")}
                                onClick={() => setShowPendingPanel(s => !s)}
                                disabled={!chatData?.modelCapabilities?.supportsTools}
                                aria-label="Pending actions"
                            >
                                <FontAwesomeIcon icon={faWrench} className="" />
                                <span className="ms-2 d-none d-md-inline">Pending</span>
                            </button>
                            <button 
                                disabled={isBusy} 
                                type="button" 
                                className="btn btn-sm btn-secondary ms-2 mb-0" 
                                aria-label="Refresh" 
                                onClick={() => refetch()}
                            >
                                <FontAwesomeIcon icon={faArrowsRotate} className="" />
                                <span className="ms-2 d-none d-md-inline">Refresh</span>
                            </button>
                            <button 
                                disabled={isBusy} 
                                type="button" 
                                className="btn btn-sm btn-secondary ms-2 mb-0" 
                                aria-label="Refresh" 
                                onClick={resubmitChat}
                            >
                                {isPendingResubmit && (<span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>)}
                                {!isPendingResubmit && <FontAwesomeIcon icon={faGroupArrowsRotate} className="" />}
                                <span className="ms-2 d-none d-md-inline">Regenerate</span>
                            </button>
                        </div>}
                </div>
            </div>
            <div className="m-2 p-2"></div>
            <div className="mt-5 pt-3 pb-2 ">
                {chatStatusFragments}
                {messagesFragment}
            </div>

            <SaveModelModal 
                isOpen={modelSaveModalOpen} 
                onClose={() => setModelSaveModalOpen(false)}
                onSave={(modelData) => {
                    // Update the newModelName and save with the model data
                    setNewModelName(modelData.name);
                    saveNewModel(modelData);
                    setModelSaveModalOpen(false);
                }}
                chatData={chatData}
                initialModelName={newModelName}
            />

            <MessageEditModal
                isOpen={editModalOpen}
                onClose={() => setEditModalOpen(false)}
                onSave={() => {
                    saveEditedMessage();
                    setEditModalOpen(false);
                }}
                messageToEdit={messageToEdit}
                setMessageToEdit={setMessageToEdit}
            />

            <ToolSelectionModal
                isOpen={toolSelectionModalOpen}
                onClose={() => setToolSelectionModalOpen(false)}
                onConfirm={async (toolIds, bagIds) => {
                    setSelectedToolIds(toolIds);
                    setSelectedBagIds(bagIds);
                    // Persist to chat options immediately (use fetch to include bag IDs)
                    if (chatId) {
                        try {
                            await updateChatOptionsFetch(chatId, {
                                temperature: chatData?.options?.temperature ?? 0.7,
                                topP: chatData?.options?.topP ?? 0.3,
                                frequencyPenalty: chatData?.options?.frequencyPenalty ?? 0.5,
                                presencePenalty: chatData?.options?.presencePenalty ?? 0.1,
                                enabledToolIds: toolIds,
                                enabledToolBagIds: bagIds,
                            });
                            toast.success(`${toolIds.length + bagIds.length} selections saved`);
                            refetch();
                        } catch {
                            toast.error('Failed to save tool selections');
                        }
                    }
                    setToolSelectionModalOpen(false);
                }}
                initialSelectedToolIds={selectedToolIds}
                initialSelectedBagIds={selectedBagIds}
            />

            <ChatOptionsModal
                isOpen={chatOptionsModalOpen}
                onClose={() => setChatOptionsModalOpen(false)}
                onSave={async (options) => {
                    if (chatId) {
                        try {
                            await updateChatOptionsFetch(chatId, { ...options, enabledToolIds: selectedToolIds, enabledToolBagIds: selectedBagIds });
                            toast.success('Options saved');
                            refetch();
                        } catch {
                            toast.error('Failed to save options');
                        }
                    }
                    setChatOptionsModalOpen(false);
                }}
                initialOptions={chatData?.options ? {
                    temperature: chatData.options.temperature ?? 0.7,
                    topP: chatData.options.topP ?? 0.3,
                    frequencyPenalty: chatData.options.frequencyPenalty ?? 0.5,
                    presencePenalty: chatData.options.presencePenalty ?? 0.1
                } : undefined}
            />

            <div className="pt-5" ref={pageBottomRef}></div>
        </div>
    );
}

export default Chat;



// Chat types
export interface ChatOptions {
    enabledToolIds?: string[];
    temperature?: number;
    topP?: number;
    frequencyPenalty?: number;
    presencePenalty?: number;
}

export interface CreateChatFormData {
    name: string;
    model: string;
    serverId: string;
    options: ChatOptions;
    includeSysMsg: boolean;
    includeUserMsg: boolean;
    systemMessage?: string;
    userMessage?: string;
}
