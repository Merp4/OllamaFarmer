import { faArrowsRotate, faWrench, faServer, faPlus, faEdit, faTrash } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { $queryClient, $fetchClient } from "../../api/api";
import { useState } from "react";
import type { components } from "../../api/schema";
import { ServerModal } from "../../components/ServerModal";
import { useDialogContext } from "../../hooks/useDialogContext";

type McpServer = components["schemas"]["McpServer"];
type ChatServerDto = components["schemas"]["ChatServerDto"];

function ToolServerList() {
    const [selectedServerId, setSelectedServerId] = useState<string | null>(null);
    const [showModal, setShowModal] = useState(false);
    const [editingServer, setEditingServer] = useState<McpServer | null>(null);
    const dialogs = useDialogContext();

    // Fetch MCP Servers
    const { data: servers, error: serversError, isFetching: isFetchingServers, isLoading: isLoadingServers, refetch: refetchServers } = 
        $queryClient.useQuery("get", "/api/ToolServer/all");

    // Fetch tools for selected server
    const { data: tools, error: toolsError, isFetching: isFetchingTools, refetch: refetchTools } = 
        $queryClient.useQuery("get", "/api/ToolServer/{serverId}/tools", {
            params: { path: { serverId: selectedServerId || "" } }
        }, {
            enabled: !!selectedServerId
        });

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

    const handleSaveServer = async (serverData: McpServer | ChatServerDto) => {
        try {
            const mcpServerData = serverData as McpServer;
            if (editingServer) {
                // Update existing server
                await $fetchClient.PUT("/api/ToolServer/{id}", {
                    params: { path: { id: editingServer.id! } },
                    body: mcpServerData
                });
            } else {
                // Create new server
                await $fetchClient.POST("/api/ToolServer", {
                    body: mcpServerData
                });
            }
            refetchServers();
            setShowModal(false);
            setEditingServer(null);
        } catch (error) {
            console.error("Failed to save server:", error);
            throw error;
        }
    };

    const handleEditServer = (server: McpServer) => {
        setEditingServer(server);
        setShowModal(true);
    };

    const handleNewServer = () => {
        setEditingServer(null);
        setShowModal(true);
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
                                <span className="badge bg-secondary">{server.mcpTools?.length || 0}</span>
                            </td>
                            <td className="text-nowrap">
                                <button 
                                    className="btn btn-sm btn-outline-primary me-1"
                                    onClick={(e) => { e.stopPropagation(); handleEditServer(server); }}
                                    title="Edit Server">
                                    <FontAwesomeIcon icon={faEdit} size="sm" />
                                </button>
                                <button 
                                    className="btn btn-sm btn-outline-danger"
                                    onClick={(e) => { e.stopPropagation(); handleDeleteServer(server.id!); }}
                                    title="Delete Server">
                                    <FontAwesomeIcon icon={faTrash} size="sm" />
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
        <div className="p-1 mt-3">
            <div className="d-flex justify-content-between align-items-center mb-2">
                <h5>Tools for Selected Server</h5>
                <button type="button" className="btn btn-sm btn-primary" onClick={() => refetchTools()}>
                    <FontAwesomeIcon icon={faArrowsRotate} className={"me-2 " + (isFetchingTools ? "refresh-rotate" : "")} />
                    Refresh Tools
                </button>
            </div>
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
                                    <span className="text-truncate">{tool.name}</span>
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
                    <button type="button" className="btn btn-primary" 
                        aria-label="New server" onClick={handleNewServer}>
                        <FontAwesomeIcon icon={faPlus} className="me-2" />
                        New Server
                    </button>
                </div>
            </div>
            <div className="container-fluid">
                {errorFragment(serversError, "servers")
                    || (!servers && !isFetchingServers && noDataFragment("No MCP servers available."))
                    || serversTableFragment}
                
                {errorFragment(toolsError, "tools") || toolsTableFragment}
            </div>

            <ServerModal
                isOpen={showModal}
                onClose={() => {
                    setShowModal(false);
                    setEditingServer(null);
                }}
                onSave={handleSaveServer}
                server={editingServer}
                serverType="mcp"
                title={editingServer ? "Edit MCP Server" : "Create New MCP Server"}
            />
        </div>
    );
}

export default ToolServerList;
