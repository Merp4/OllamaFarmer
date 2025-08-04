import { faArrowsRotate, faComments, faPlus, faEdit, faTrash, faCog } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { $queryClient, $fetchClient } from "../../api/api";
import { useState } from "react";
import type { components } from "../../api/schema";
import { ServerModal } from "../../components/ServerModal";
import type { McpServer } from "../../api/apiTypes";
import type { BaseModalProps } from "../../utils/common";
import { useDialogContext } from "../../hooks/useDialogContext";

// Server management types
export interface ServerFormData {
    name: string;
    uri: string;
    description?: string;
    enabled: boolean;
}

export interface ServerModalProps extends BaseModalProps {
    editingServer?: McpServer | ChatServerDto | null;
    onSave: (serverData: ServerFormData) => void;
    serverType: 'mcp' | 'chat' | 'tool';
}
type ChatServerDto = components["schemas"]["ChatServerDto"];

function ChatServerList() {
    const [selectedServerId, setSelectedServerId] = useState<string | null>(null);
    const [showModal, setShowModal] = useState(false);
    const [editingServer, setEditingServer] = useState<ChatServerDto | null>(null);
    const dialogs = useDialogContext();

    // Fetch Chat Servers
    const { data: servers, error: serversError, isFetching: isFetchingServers, isLoading: isLoadingServers, refetch: refetchServers } = 
        $queryClient.useQuery("get", "/api/ChatServer/all");

    const placeholderRow = (colCount: number) => <tr>
        {[...Array(colCount)].map((_, i) =>
            <td key={i} className="placeholder-glow">
                <span className="placeholder col-6 m-2 bg-primary"></span>
            </td>
        )}
    </tr>;

    const errorFragment = (error: unknown, context: string) => error ? 
        <div className="d-inline-flex card bg-danger-subtle" aria-hidden="true">
            <div className="card-body">
                <h5 className="card-title">Error</h5>
                <p className="card-text">Failed to retrieve {context}, response: <code>{JSON.stringify(error)}</code></p>
            </div>
        </div> : null;

    const noDataFragment = (message: string) =>
        <div className="card bg-warning-subtle" aria-hidden="true">
            <div className="card-body">
                <h5 className="card-title">No data</h5>
                <p className="card-text">{message}</p>
            </div>
        </div>;

    const handleEditServer = (server: ChatServerDto) => {
        setEditingServer(server);
        setShowModal(true);
    };

    const handleCreateServer = () => {
        setEditingServer(null);
        setShowModal(true);
    };

    const handleSaveServer = async (serverData: ChatServerDto) => {
        try {
            if (editingServer) {
                // Update existing server
                await $fetchClient.PUT("/api/ChatServer/{id}", {
                    params: { path: { id: editingServer.id! } },
                    body: serverData
                });
            } else {
                // Create new server
                await $fetchClient.POST("/api/ChatServer", {
                    body: serverData
                });
            }
            refetchServers();
        } catch (error) {
            console.error("Failed to save server:", error);
            throw error;
        }
    };

    const handleDeleteServer = async (serverId: string) => {
        dialogs.showDangerConfirmDialog(
            "Delete Chat Server",
            "Are you sure you want to delete this chat server? This action cannot be undone.",
            async () => {
                try {
                    await $fetchClient.DELETE("/api/ChatServer/{id}", {
                        params: { path: { id: serverId } }
                    });
                    refetchServers();
                    if (selectedServerId === serverId) {
                        setSelectedServerId(null);
                    }
                } catch (error) {
                    console.error("Failed to delete server:", error);
                    dialogs.showErrorDialog(
                        "Error",
                        "Failed to delete the chat server. Please try again."
                    );
                }
            }
        );
    };

    const serversTableFragment = servers &&
        <div className="p-1">
            <table className="table table-striped table-hover" aria-labelledby="tableLabel">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Description</th>
                        <th>URI</th>
                        <th>Version</th>
                        <th>Created</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {isLoadingServers && placeholderRow(6)}
                    {servers?.map((server: ChatServerDto) => (
                        <tr key={server.id} 
                            className={selectedServerId === server.id ? "table-active" : ""}
                            style={{ cursor: "pointer" }}
                            onClick={() => setSelectedServerId(server.id || null)}>
                            <td className="text-nowrap">
                                <div className="d-flex flex-row align-items-center">
                                    <FontAwesomeIcon icon={faComments} className="me-2" />
                                    <span className="text-truncate">{server.name}</span>
                                </div>
                            </td>
                            <td className="text-truncate">{server.description || "No description"}</td>
                            <td className="text-truncate">
                                <code>{server.uri}</code>
                            </td>
                            <td className="text-truncate">{server.version || "N/A"}</td>
                            <td className="text-truncate">
                                {server.createdAt ? new Date(server.createdAt).toLocaleDateString() : "N/A"}
                            </td>
                            <td className="text-nowrap">
                                <button 
                                    className="btn btn-sm btn-primary me-2"
                                    onClick={(e) => { e.stopPropagation(); dialogs.showFeatureUnavailableDialog("Server configuration"); }}
                                    title="Configure Server">
                                    <FontAwesomeIcon icon={faCog} size="sm" className="me-2" />
                                    Configure
                                </button>
                                <button 
                                    className="btn btn-sm btn-secondary me-2"
                                    onClick={(e) => { e.stopPropagation(); handleEditServer(server); }}
                                    title="Edit Server">
                                    <FontAwesomeIcon icon={faEdit} size="sm" className="me-2" />
                                    Edit
                                </button>
                                <button 
                                    className="btn btn-sm btn-danger me-2"
                                    onClick={(e) => { e.stopPropagation(); handleDeleteServer(server.id!); }}
                                    title="Delete Server">
                                    <FontAwesomeIcon icon={faTrash} size="sm" className="me-2" />
                                    Delete
                                </button>
                            </td>
                        </tr>
                    ))}
                    {isFetchingServers && placeholderRow(6)}
                    {servers?.length === 0 && <tr>
                        <td colSpan={6} className="text-center text-muted">No chat servers found.</td>
                    </tr>}
                </tbody>
            </table>
        </div>;

    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">Chat Servers</h1>
                <div className="d-flex flex-row p-1">
                    <button type="button" disabled={isFetchingServers} className="btn btn-primary me-3" 
                        aria-label="Refresh servers" onClick={() => refetchServers()}>
                        <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetchingServers ? "refresh-rotate" : "")} />
                        Refresh
                    </button>
                    <button type="button" className="btn btn-primary" 
                        aria-label="New server" onClick={handleCreateServer}>
                        <FontAwesomeIcon icon={faPlus} className="me-2" />
                        New Server
                    </button>
                </div>
            </div>
            <div className="container-fluid">
                {errorFragment(serversError, "servers")
                    || (!servers && !isFetchingServers && noDataFragment("No chat servers available."))
                    || serversTableFragment}
            </div>

            <ServerModal
                isOpen={showModal}
                onClose={() => setShowModal(false)}
                onSave={handleSaveServer}
                server={editingServer}
                serverType="chat"
                title={editingServer ? "Edit Chat Server" : "Create Chat Server"}
            />
        </div>
    );
}

export default ChatServerList;
