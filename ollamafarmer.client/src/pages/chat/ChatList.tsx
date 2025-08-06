import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { $queryClient } from "../../api/api";
import { faArrowDown, faComments, faPlus, faTrash } from "@fortawesome/free-solid-svg-icons";
import type { components } from "../../api/schema";
import { NavLink } from "react-router";
import { useState, useEffect } from "react";
import { Modal, ModalHeader, ModalBody, ModalFooter, Button, Input, Label } from "reactstrap";
import CreateChatModal from "./components/CreateChatModal";
import { faCopy } from "@fortawesome/free-solid-svg-icons/faCopy";
import { faRobot } from "@fortawesome/free-solid-svg-icons/faRobot";
import ChatServerSelector from "../../components/ChatServerSelector";
import LoadingPanel from "../../components/LoadingPanel";
import { useDialogContext } from "../../hooks/useDialogContext";
import SaveModelModal from "./components/SaveModelModal";
import { toast } from "react-toastify";

//type CreateChatRequest = components["schemas"]["ExpoCreateChatRequest"];

function ChatList() {

    const [selectedServerId, setSelectedServerId] = useState<string>("");
    const dialogs = useDialogContext();

    // Fetch chat servers to enable auto-selection
    const { data: servers } = $queryClient.useQuery(
        "get", 
        "/api/ChatServer/all", 
        {}, 
        {}
    );

    // Auto-select if there's only one server and no selection yet
    useEffect(() => {
        if (servers && servers.length === 1 && selectedServerId === "") {
            setSelectedServerId(servers[0].id!);
        }
    }, [servers, selectedServerId]);

    type ChatDetails = components["schemas"]["AppChatDetails"];

    const pageSize = 20;

    type PageType = { cursor: number | null; filteredCount: number | null; chatDetails?: ChatDetails[] };

    const { data, error, fetchNextPage, hasNextPage, isFetching, isLoading } = $queryClient.useInfiniteQuery(
        "get", 
        "/api/AppChat", 
        { 
            params: { 
                query: { 
                    pageSize: pageSize, serverId: selectedServerId === "" ? undefined : selectedServerId
                 } 
            } 
        }, 
        {
            getNextPageParam: (lastPage: PageType) =>
                pageSize * ((lastPage.cursor ?? 0) + 1) >= (lastPage.filteredCount ?? 0)
                    ? undefined : (lastPage.cursor ?? 0) + 1,
            getPreviousPageParam: (firstPage: PageType) => firstPage.cursor ?? 0,
            initialPageParam: 0,
            //enabled: !!selectedServerId
        });

    const [createChatModalIsOpen, setCreateChatModalIsOpen] = useState(false);
    const [saveModelModalOpen, setSaveModelModalOpen] = useState(false);
    const [selectedChatForModel, setSelectedChatForModel] = useState<ChatDetails | null>(null);
    const [cloneDialogOpen, setCloneDialogOpen] = useState(false);
    const [selectedChatForClone, setSelectedChatForClone] = useState<ChatDetails | null>(null);
    const [cloneChatName, setCloneChatName] = useState("");

    // Fetch full chat data when selectedChatForModel changes
    const { data: fullChatDataForModel } = $queryClient.useQuery(
        "get",
        "/api/AppChat/{id}",
        { params: { path: { id: selectedChatForModel?.id || "" } } },
        {
            enabled: !!selectedChatForModel?.id,
            refetchOnWindowFocus: false
        }
    );



    // API mutation for creating models from chat
    const { mutateAsync: mutateChatModelAsync } = $queryClient.useMutation(
        "post", 
        "/api/AppChat/{id}/create-model", 
        {    
            onMutate: () => {
                toast.info("Creating model...");
            },
            onSuccess: (response) => {
                toast.success("Model saved as " + response?.model);
            },
            onError: (error) => {
                toast.error("Error saving model: " + JSON.stringify(error, [], 2));
            },
        }
    );

    // API mutation for cloning chat using the backend clone endpoint
    const { mutateAsync: mutateCloneChatAsync } = $queryClient.useMutation(
        "post", 
        "/api/AppChat/{id}/clone", 
        {    
            onMutate: () => {
                toast.info("Cloning chat...");
            },
            onSuccess: () => {
                toast.success("Chat cloned successfully!");
                // Refresh the chat list
                window.location.reload(); // Simple refresh for now
            },
            onError: (error) => {
                toast.error("Error cloning chat: " + JSON.stringify(error, [], 2));
            },
        }
    );

    const handleDeleteChat = (chat: ChatDetails) => {
        dialogs.showDangerConfirmDialog(
            "Delete Chat",
            `Are you sure you want to delete "${chat.name || '[Unnamed Chat]'}"? This action cannot be undone.`,
            () => {
                // TODO: Implement chat deletion API
                dialogs.showFeatureUnavailableDialog("Chat deletion");
            }
        );
    };

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const handleCloneChat = (chat: ChatDetails) => {
        setSelectedChatForClone(chat);
        setCloneChatName(`Copy of ${chat.name || "Unnamed Chat"}`);
        setCloneDialogOpen(true);
    };

    const handleCloneChatConfirm = async () => {
        if (!selectedChatForClone?.id) {
            toast.error("No chat selected for cloning");
            return;
        }

        try {
            // Use the backend clone endpoint that handles all the complexity
            await mutateCloneChatAsync({
                params: { 
                    path: { id: selectedChatForClone.id }
                },
                body: {
                    name: cloneChatName
                }
            });

            // Close the dialog
            setCloneDialogOpen(false);
            setSelectedChatForClone(null);
            setCloneChatName("");
        } catch (error) {
            console.error("Error cloning chat:", error);
        }
    };

    const handleSaveModel = (chat: ChatDetails) => {
        setSelectedChatForModel(chat);
        setSaveModelModalOpen(true);
    };

    const handleSaveModelFromModal = async (modelData: {
        name: string;
        description?: string;
    }) => {
        if (!selectedChatForModel?.id) {
            toast.error("No chat selected");
            return;
        }

        // For now, use basic chat details since we don't have full chat data
        // In a future enhancement, we could fetch the full chat data
        const systemMsg = modelData.description || "";
        
        await mutateChatModelAsync({
            body: {
                model: selectedChatForModel.model || "",
                newModelName: modelData.name,
                template: "",
                systemMessage: systemMsg,
                topP: 1.0, 
                temperature: 0.7,
            },
            params: { path: { id: selectedChatForModel.id } }
        });

        setSaveModelModalOpen(false);
        setSelectedChatForModel(null);
    };



    const placeholderRow = <tr>
        {[...Array(5)].map((_, i) =>
            <td key={i} className="placeholder-glow">
                <span className="placeholder col-6 m-2 bg-primary"></span>
            </td>
        )}
    </tr>;

    const errorFragment = error && !isFetching &&
        <div className="d-inline-flex card bg-danger-subtle" aria-hidden="true">
            <div className="card-body">
                <h5 className="card-title">Error</h5>
                <p className="card-text">Failed to retrieve data, response: <code>{JSON.stringify(error)}</code></p>
            </div>
        </div>;



    const tableDataFragment = data &&
        <div className="p-1">
            <table className="table table-striped table-hover" aria-labelledby="tableLabel">
                <thead className="">
                    <tr className="">
                        <th>Chat</th>
                        <th>Model</th>
                        <th>Server</th>
                        <th>Created</th>
                        <th className="text-end">
                        </th>
                    </tr>
                </thead>
                <tbody >
                    {isLoading && placeholderRow}
                    {data.pages.map(page => {
                        return page?.chatDetails?.map((chat: ChatDetails) => {
                            return (
                                <tr key={chat.id}>
                                    <td className={chat.name ? "" : "text-muted"}>
                                        <NavLink color="light" to={`/chat/${chat.id}`}> {chat.name ?? "[Unnamed Chat]"}</NavLink>
                                        </td>
                                    <td>{chat.model}</td>
                                    <td>{chat.serverName}</td>
                                    <td>{chat.createdAt && (
                                        <>
                                            <span className="me-2"></span>
                                            {new Date(chat.createdAt).toLocaleDateString()}
                                        </> || <span className="text-muted">Unknown</span>)}</td>
                                    <td className="text-end">
                                        <NavLink className="btn btn-sm ms-3 btn-primary" color="primary" to={`/chat/${chat.id}`}><FontAwesomeIcon icon={faComments} className="me-2 " size="sm" />Open</NavLink>

                                        <button type="button" className="btn btn-sm ms-3 btn-secondary" aria-label="Clone" onClick={() => handleCloneChat(chat)}>
                                            <FontAwesomeIcon icon={faCopy} className="me-2 " size="sm" />Clone
                                        </button>
                                        <button type="button" className="btn btn-sm ms-3 btn-info" aria-label="Create Model" onClick={() => handleSaveModel(chat)}>
                                            <FontAwesomeIcon icon={faRobot} className="me-2 " size="sm" />Save Model
                                        </button>
                                        <button type="button" className="btn btn-sm ms-3 btn-danger" aria-label="Delete" onClick={() => handleDeleteChat(chat)}>
                                            <FontAwesomeIcon icon={faTrash} className="me-2 " size="sm" />Delete</button>
                                    </td>
                                </tr>
                            );
                        });
                    }
                    )}
                    {(isFetching || isLoading)
                        && placeholderRow
                        && placeholderRow
                        && placeholderRow
                        && placeholderRow}
                    {data.pages.length === 0 || data.pages[0]?.filteredCount === 0 && <tr>
                        <td colSpan={5} className="text-center text-muted">No chats found.</td>
                    </tr>}
                </tbody>
            </table>
            {(hasNextPage && !isFetching) && <div className="d-flex flex-row mt-1 justify-content-center">
                <div className="d-flex h-100 text-center mt-3">
                    <button disabled={!hasNextPage || isFetching} type="button" className="btn btn-sm btn-secondary"
                        aria-label="Load more" onClick={() => fetchNextPage()}>
                        <FontAwesomeIcon icon={faArrowDown} className={"me-2 " + (isFetching ? "refresh-rotate" : "")} size="sm" />
                        Load more</button>
                </div>
            </div>}
        </div>;


    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">Chats</h1>

                {/* Chat Server Selection */}
                <div className="d-flex flex-row p-1 mb-3">
                    <div className="me-3">
                        <ChatServerSelector
                            selectedServerId={selectedServerId}
                            onServerChange={setSelectedServerId}
                            showAllOption={servers ? servers.length > 1 : true}
                            allOptionLabel="All"
                            className="d-inline w-auto"
                        />
                    </div>
                </div>

                {<div className="d-flex flex-row p-1">
                    <button 
                        type="button" 
                        className="btn btn-primary" 
                        aria-label="New chat" 
                        onClick={() => setCreateChatModalIsOpen(true)}
                    >
                        <FontAwesomeIcon icon={faPlus} className="me-2 " />New Chat
                    </button>
                </div>}
            </div>
            <div className="container-fluid">
                {errorFragment
                    || isLoading && <LoadingPanel />     
                    //|| noDataFragment
                    || tableDataFragment}
            </div>
            {createChatModalIsOpen && 
                <CreateChatModal 
                    isOpen={createChatModalIsOpen} 
                    toggle={() => setCreateChatModalIsOpen(false)} 
                    serverId={selectedServerId}
                />}
            
            <SaveModelModal 
                isOpen={saveModelModalOpen}
                onClose={() => {
                    setSaveModelModalOpen(false);
                    setSelectedChatForModel(null);
                }}
                onSave={handleSaveModelFromModal}
                chatData={fullChatDataForModel}
                initialModelName={selectedChatForModel?.name || `${selectedChatForModel?.model || 'Model'} - ${new Date().toLocaleDateString()}`}
            />

            {/* Clone Chat Modal */}
            <Modal isOpen={cloneDialogOpen} toggle={() => {
                setCloneDialogOpen(false);
                setSelectedChatForClone(null);
                setCloneChatName("");
            }}>
                <ModalHeader toggle={() => {
                    setCloneDialogOpen(false);
                    setSelectedChatForClone(null);
                    setCloneChatName("");
                }}>
                    Clone Chat
                </ModalHeader>
                <ModalBody>
                    <p>This will create a copy of "<strong>{selectedChatForClone?.name || "Unnamed Chat"}</strong>" with all its messages and settings.</p>
                    <Label for="cloneChatName">New Chat Name:</Label>
                    <Input
                        id="cloneChatName"
                        type="text"
                        value={cloneChatName}
                        onChange={(e) => setCloneChatName(e.target.value)}
                        placeholder="Enter name for cloned chat"
                    />
                </ModalBody>
                <ModalFooter>
                    <Button 
                        color="primary" 
                        onClick={handleCloneChatConfirm}
                        disabled={!cloneChatName.trim()}
                    >
                        <FontAwesomeIcon icon={faCopy} className="me-2" />
                        Clone Chat
                    </Button>
                    <Button 
                        color="secondary" 
                        onClick={() => {
                            setCloneDialogOpen(false);
                            setSelectedChatForClone(null);
                            setCloneChatName("");
                        }}
                    >
                        Cancel
                    </Button>
                </ModalFooter>
            </Modal>
        </div>
    );
}

export default ChatList;
