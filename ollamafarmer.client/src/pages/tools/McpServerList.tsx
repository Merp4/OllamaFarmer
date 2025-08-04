import { faArrowsRotate, faWrench, faServer, faPlus, faEdit, faTrash } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { $queryClient, $fetchClient } from "../../api/api";
import { useState } from "react";
import type { components } from "../../api/schema";
import { ServerModal } from "../../components/ServerModal";
import { toast } from "react-toastify";
import { faSync } from "@fortawesome/free-solid-svg-icons/faSync";
import { useDialogContext } from "../../hooks/useDialogContext";


type McpServer = components["schemas"]["McpServer"];

function ToolServerList() {
    const [selectedServerId, setSelectedServerId] = useState<string | null>(null);
    const [showModal, setShowModal] = useState(false);
    const dialogs = useDialogContext();
    const [editingServer, setEditingServer] = useState<McpServer | null>(null);

    // Fetch MCP Servers
    const { data: servers, error: serversError, isFetching: isFetchingServers, isLoading: isLoadingServers, refetch: refetchServers } = 
        $queryClient.useQuery("get", "/api/ToolServer/all");

    // Fetch tools for selected server
    const { data: tools, error: toolsError, isFetching: isFetchingTools, isLoading: isLoadingTools, refetch: refetchTools } =
        $queryClient.useQuery("get", "/api/ToolServer/{serverId}/tools", {
            params: { path: { serverId: selectedServerId || "" } }
        }, {
            enabled: !!selectedServerId,
        });
    
    // Sync Specific MCP Server
    const syncServer = async (serverId: string) => {
        try {
            await $fetchClient.GET("/api/ToolServer/{serverId}/sync", {
                params: { path: { serverId } }
            });
            refetchServers();
        } catch (error) {
            console.error("Failed to sync server:", error);
        } finally {
            refetchServers();
            refetchTools();
        }
    };
    // Sync All MCP Servers
    const syncAllServers = async () => {
        try {
            await $fetchClient.GET("/api/ToolServer/sync");
            toast.success("Synchronization started. You will be notified when it completes.");
        } catch (error) {
            console.error("Failed to sync all servers:", error);
        } finally {
            refetchServers();
            refetchTools();
        }
    };

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
        <div className="" aria-hidden="true">
                <h5 className="card-title">No data</h5>
                <p className="card-text">{message}</p>
        </div>;

    const handleEditServer = (server: McpServer) => {
        setEditingServer(server);
        setShowModal(true);
    };

    const handleCreateServer = () => {
        setEditingServer(null);
        setShowModal(true);
    };

    const handleSaveServer = async (serverData: McpServer | { id?: string; name?: string; description?: string | null; version?: string | null; createdAt?: string; updatedAt?: string; uri?: string; mcpTools?: [] }) => {
        // Ensure uri is present and is a string
        if (!serverData.uri || typeof serverData.uri !== "string") {
            throw new Error("Server URI is required.");
        }
        
        // Create a properly typed server object for the API
        const serverPayload = {
            ...serverData,
            uri: serverData.uri // This is now guaranteed to be a string
        };
        
        try {
            if (editingServer) {
                // Update existing server
                await $fetchClient.PUT("/api/ToolServer/{id}", {
                    params: { path: { id: editingServer.id! } },
                    body: serverPayload
                });
            } else {
                // Create new server
                await $fetchClient.POST("/api/ToolServer", {
                    body: serverPayload
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
            "Delete MCP Server",
            "Are you sure you want to delete this MCP server? This action cannot be undone.",
            async () => {
                try {
                    await $fetchClient.DELETE("/api/ToolServer/{id}", {
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
                        "Failed to delete the MCP server. Please try again."
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
                        <th>Tools</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {isLoadingServers && placeholderRow(5)}
                    {servers?.map((server: McpServer) => (
                        <tr key={server.id} 
                            className={selectedServerId === server.id ? "table-active" : ""}
                            style={{ cursor: "pointer" }}
                            onClick={() => setSelectedServerId(server.id || null)}>
                            <td className="text-nowrap">
                                <div className="d-flex flex-row align-items-center">
                                    <FontAwesomeIcon icon={faServer} className="me-2" />
                                    <span className="text-truncate">{server.name}</span>
                                </div>
                            </td>
                            <td className="text-truncate">{server.description || "No description"}</td>
                            <td className="text-truncate">
                                <code>{server.uri}</code>
                            </td>
                            <td className="text-center">
                                <span className={"badge " + ((server.mcpTools?.length || 0) > 0 ? "bg-primary" : "bg-warning")}>{server.mcpTools?.length || 0}</span>
                            </td>
                            <td className="text-nowrap">
                                <button
                                    className="btn btn-sm btn-primary me-2"
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
                    {isFetchingServers && placeholderRow(5)}
                    {servers?.length === 0 && <tr>
                        <td colSpan={5} className="text-center text-muted">No MCP servers found.</td>
                    </tr>}
                </tbody>
            </table>
        </div>;

    const toolsTableFragment = selectedServerId && tools ?
        <div className="p-1 ms-3">
            {/* <div className="d-flex justify-content-between align-items-center mb-2"> */}
                <h5>Tools available for {servers?.find(server => server.id === selectedServerId)?.name ?? ""}</h5>
                <button type="button" className="btn btn-sm btn-warning" onClick={() => syncServer(selectedServerId)}>
                    <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetchingTools ? "refresh-rotate" : "")} />
                    Synchronise Tools
                </button>
            {/* </div> */}
            <table className="table table-striped table-hover table-sm">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Description</th>
                        <th>Version</th>
                        <th>Created</th>
                    </tr>
                </thead>
                <tbody>
                    {isFetchingTools && placeholderRow(4)}
                    {tools?.map((tool) => (
                        <tr key={tool.id}>
                            <td className="text-nowrap">
                                <div className="d-flex flex-row align-items-center">
                                    <FontAwesomeIcon icon={faWrench} className="me-2" />
                                    <code className="">{tool.name}</code>
                                </div>
                            </td>
                            <td className="text-truncate">{tool.description || "No description"}</td>
                            <td className="text-truncate">{tool.version || "N/A"}</td>
                            <td className="text-truncate">
                                {tool.createdAt ? new Date(tool.createdAt).toLocaleDateString() : "N/A"}
                            </td>
                        </tr>
                    ))}
                    {tools?.length === 0 && <tr>
                        <td colSpan={4} className="text-center text-muted">No tools found for this server.</td>
                    </tr>}
                </tbody>
            </table>
        </div> : null;

    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">MCP Tool Servers</h1>
                <div className="d-flex flex-row p-1">
                    <button type="button" disabled={isFetchingServers} className="btn btn-primary me-3" 
                        aria-label="Refresh servers" onClick={() => refetchServers()}>
                        <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetchingServers ? "refresh-rotate" : "")} />
                        Refresh
                    </button>
                    <button type="button" className="btn btn-primary me-3" 
                        aria-label="New server" onClick={handleCreateServer}>
                        <FontAwesomeIcon icon={faPlus} className="me-2" />
                        New Server
                    </button>
                    <button type="button" className="btn btn-warning me-3" 
                        aria-label="Sync all servers" onClick={syncAllServers}>
                        <FontAwesomeIcon icon={faSync} className="me-2" />
                        Synchronise Tools Across All Servers
                    </button>
                </div>
            </div>
            <div className="container-fluid">
                {errorFragment(serversError, "servers")
                    || (!servers && !isFetchingServers && noDataFragment("No MCP servers available."))
                    || serversTableFragment}
                
                {errorFragment(toolsError, "tools") 
                    || (selectedServerId && !tools && !isFetchingTools && noDataFragment("No tools available for the selected server."))
                    || (selectedServerId && isLoadingTools && placeholderRow(4))
                    || toolsTableFragment}
            </div>

            <ServerModal
                isOpen={showModal}
                onClose={() => setShowModal(false)}
                onSave={handleSaveServer}
                server={editingServer}
                serverType="mcp"
                title={editingServer ? "Edit MCP Server" : "Create MCP Server"}
            />
        </div>
    );
}

export default ToolServerList;
