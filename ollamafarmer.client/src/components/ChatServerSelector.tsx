import { Input } from "reactstrap";
import { $queryClient } from "../api/api";
import { useEffect } from "react";

interface ChatServerSelectorProps {
    selectedServerId: string;
    onServerChange: (serverId: string) => void;
    showAllOption?: boolean;
    allOptionLabel?: string;
    placeholder?: string;
    className?: string;
    style?: React.CSSProperties;
}

function ChatServerSelector({
    selectedServerId,
    onServerChange,
    showAllOption = false,
    allOptionLabel = "All",
    placeholder = "Select a chat server...",
    className = "d-inline w-auto",
    style
}: ChatServerSelectorProps) {
    // Fetch chat servers
    const { data: servers, error: serversError, isLoading: isLoadingServers } = $queryClient.useQuery(
        "get", 
        "/api/ChatServer/all", 
        {}, 
        {}
    );

    // Auto-select if there's only one server and no selection yet
    useEffect(() => {
        if (servers && servers.length === 1 && !selectedServerId && !showAllOption) {
            onServerChange(servers[0].id!);
        }
    }, [servers, selectedServerId, onServerChange, showAllOption]);

    if (isLoadingServers) {
        return (
            <div className="d-flex align-items-center">
                <label htmlFor="chatServer" className="d-inline form-label me-2">Chat Server</label>
                <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
            </div>
        );
    }

    if (serversError) {
        return (
            <div className="d-flex align-items-center">
                <label htmlFor="chatServer" className="d-inline form-label me-2">Chat Server</label>
                <div className="text-danger">Failed to load chat servers</div>
            </div>
        );
    }

    if (!servers) {
        return null;
    }

    // Hide the selector if there's only one server and it's already selected
    if (servers.length === 1 && !showAllOption && selectedServerId === servers[0].id) {
        return (
            <div className="border-bottom border-bottom-1 border-opacity-50 border-primary">
                <label className="m-1"><h6 className="d-inline">Chat Server</h6></label>
                <div className="d-inline ms-2 text-muted">
                    {servers[0].name} - {servers[0].uri}
                </div>
            </div>
        );
    }

    return (
        // <div className="border border-opacity-25 border-2 border-primary rounded p-1 px-2 m-0">
        <div className="border-bottom border-bottom-1 border-opacity-50 border-primary">
            <label htmlFor="chatServer" className="m-1"><h6 className="d-inline">Chat Server</h6></label>
            <Input
                id="chatServer"
                name="select"
                type="select"
                value={selectedServerId}
                className={className}
                style={style}
                onChange={(e) => onServerChange(e.target.value)}
            >
                {showAllOption && <option value="">{allOptionLabel}</option>}
                {!showAllOption && <option value="">{placeholder}</option>}
                {servers.map((server) => (
                    <option key={server.id} value={server.id}>
                        {server.name} - {server.uri}
                    </option>
                ))}
            </Input>
        </div>
    );
}

export default ChatServerSelector;
