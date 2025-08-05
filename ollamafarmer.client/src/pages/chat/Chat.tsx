import './Chat.scss';
import { useEffect, useRef, useState, useCallback, useMemo } from "react";
import debounce from "lodash/debounce";
import { useParams } from 'react-router';
import { Button } from "reactstrap";
import { toast } from 'react-toastify';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faRobot, faFireAlt, faArrowsRotate, faBrain, faWrench, faGroupArrowsRotate, faGear } from '@fortawesome/free-solid-svg-icons';

import { useNotificationHub } from '../../context/useNotificationHub';
import type { ClientNotification } from '../../api/notificationHub';

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
import { $queryClient } from '../../api/api';

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

    // Get all models first to find the matching model ID
    const { data: allModels } = $queryClient.useQuery(
        "get", 
        "/api/ChatModel/all", 
        { params: { query: {} } }, 
        {
            enabled: !!chatData?.model,
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

    // Chat options mutation
    const { mutate: updateChatOptions } = $queryClient.useMutation(
        "put",
        "/api/AppChat/{id}/options",
        {
            onSuccess: () => {
                toast.success("Chat options updated successfully");
                refetch(); // Refresh chat data to show updated options
            },
            onError: (error) => {
                console.error("Failed to update chat options:", error);
                toast.error("Failed to update chat options");
            }
        }
    );

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

    useEffect(() => {
        if (connectionRef) {
            const handleMessage = (msg: ClientNotification) => {
                toast(msg.message, { type: msg.type });
            };

            connectionRef.on('chat', handleMessage);

            return () => {
                connectionRef.off('chat', handleMessage);
            };
        }
    }, [connectionRef]);

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
    ), [chatId, chatData, awaitingReply, showThoughts, deleteMessage, message, setMessage, role, setRole, isBusy, isPendingAdd, isPendingSend, addMessage, sendMessage, editMessage, setAttachment]);




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
                                color={chatData?.modelCapabilities?.supportsTools ? selectedToolIds.length > 0 ? "success" : "secondary" : "outline-secondary"} 
                                className="btn small btn-sm ms-2"
                                disabled={!chatData?.modelCapabilities?.supportsTools}                                    
                                onClick={() => setToolSelectionModalOpen(true)}
                            >
                                <FontAwesomeIcon icon={faWrench} className='small' />
                                <span className="ms-2 d-none d-md-inline">Tools ({selectedToolIds.length})</span>
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
                onConfirm={(selectedToolIds) => {
                    setSelectedToolIds(selectedToolIds);
                    setToolSelectionModalOpen(false);
                    toast.success(`${selectedToolIds.length} tools selected`);
                }}
                initialSelectedToolIds={selectedToolIds}
            />

            <ChatOptionsModal
                isOpen={chatOptionsModalOpen}
                onClose={() => setChatOptionsModalOpen(false)}
                onSave={(options) => {
                    if (chatId) {
                        updateChatOptions({
                            params: { path: { id: chatId } },
                            body: options
                        });
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
