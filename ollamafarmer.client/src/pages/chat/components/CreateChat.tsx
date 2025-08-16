//import { $api } from "../api/api";

import { useEffect, useRef, useState, type RefObject } from 'react';
import { $queryClient } from "../../../api/api";
import { Input, Spinner } from 'reactstrap';
import type { CreateChatRequest } from '../../../api/chatTypes';
import { ToolSelectionModal } from '../../../components/ToolSelectionModal';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTools } from '@fortawesome/free-solid-svg-icons';
import { formatFileSize } from '../../../utils/fileUtils';




export interface CreateChatProps {
    id?: string;
    //mode: "create" | "edit";
    model?: string;
    serverId?: string;
    
    parentRef: RefObject<CreateChatHandles | null>;
    onSubmitted: (chat: CreateChatRequest) => Promise<void>;
}

/** Functions exposed to the parent component through useRef */
export type CreateChatHandles = {
  submitForm: () => void;
};


const ChatForm = (props: CreateChatProps) => {

    const formRef = useRef<HTMLFormElement>(null);
    const ref = useRef<CreateChatHandles>({
        submitForm: () => {
            formRef.current?.requestSubmit();
        }
    });

    useEffect(() => {
        if (props.parentRef) {
        props.parentRef.current = ref.current;
        }
    }, [props.parentRef]);
    // Do edit later
    // const { id } = useParams<{ id: string }>();



    const [chatName, setChatName] = useState("New Chat " + Date.now().toString().slice(-6));
    const [model, setModel] = useState<string | undefined>(props.model);

    const [temperature, setTemperature] = useState(0.7);
    const [topP, setTopP] = useState(0.3);
    const [frequencyPenalty, setFrequencyPenalty] = useState(0.5);
    const [presencePenalty, setPresencePenalty] = useState(0.1);

    const [selectedServerId, setSelectedServerId] = useState<string>(props.serverId ?? "");
    const [showToolModal, setShowToolModal] = useState(false);
    const [selectedToolIds, setSelectedToolIds] = useState<string[]>([]);
    const [selectedBagIds, setSelectedBagIds] = useState<string[]>([]);

    // Fetch chat servers
    const { data: servers, error: serversError, isLoading: isLoadingServers } = $queryClient.useQuery("get", "/api/ChatServer/all", {}, { });

    // Auto-select if there's only one server and no selection yet and not overridden by props
    useEffect(() => {
        if (servers && servers.length === 1 && selectedServerId === "" && !props.serverId) {
            setSelectedServerId(servers[0].id!);
        }
    }, [servers, selectedServerId, props.serverId]);

    // Fetch models for selected server
    const { data, error, isLoading, isFetching, refetch } = $queryClient.useQuery("get", "/api/ChatModel/available", { 
        params: { query: { serverId: selectedServerId || undefined } }
    }, { 
        enabled: !!selectedServerId 
    });

    // Auto-select model if there's only one available or use props.model
    useEffect(() => {
        if (props.model && !model) {
            setModel(props.model);
        } else if (data && data.length === 1 && !model) {
            setModel(data[0].name!);
        }
    }, [data, model, props.model]);

    // Check if the selected model supports tools (MCP tools)
    const [modelSupportsTools, setModelSupportsTools] = useState<boolean | null>(null);

    // Query for model capabilities
    const { data: modelCapabilities, isLoading: isLoadingCapabilities, error: capabilitiesError } = $queryClient.useQuery(
        "get", 
        "/api/ChatModel/capabilities", 
        {
            params: {
                query: {
                    serverId: selectedServerId,
                    model: model
                }
            }
        },
        {
            enabled: !!(selectedServerId && model), // Only run query when both are available
            retry: false // Don't retry on failure
        }
    );

    // Update modelSupportsTools when capabilities data changes
    useEffect(() => {
        if (modelCapabilities) {
            setModelSupportsTools(modelCapabilities.supportsTools || false);
        } else if (!selectedServerId || !model) {
            setModelSupportsTools(null);
        } else if (!isLoadingCapabilities) {
            // Query failed or returned undefined
            setModelSupportsTools(false);
        }
    }, [modelCapabilities, selectedServerId, model, isLoadingCapabilities]);

    const createChat = (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        if (!selectedServerId) {
            alert("Please select a chat server");
            return;
        }
        props.onSubmitted({
                serverId: selectedServerId, 
                model: model, 
                name: chatName, 
                temperature: temperature,
                topP: topP,
                frequencyPenalty: frequencyPenalty,
                presencePenalty: presencePenalty,
                enabledToolIds: selectedToolIds,
                enabledToolBagIds: selectedBagIds,
            } as CreateChatRequest & { enabledToolIds?: string[]; enabledToolBagIds?: string[] });
    };



    return (
        <div className="container-fluid">
            <form ref={formRef} className="" onSubmit={createChat}>
                <div className="d-inline-flex">
                    <div className="m-2">
                        <label htmlFor="chatName" className="form-label">Chat Name</label>
                        <input required type="text" className="form-control" id="chatName" placeholder="Enter chat name" value={chatName} onChange={(e) => setChatName(e.target.value)} />
                    </div>
                </div>
                <div className="d-inline-flex">
                    <div className="m-2">
                        <label htmlFor="chatServer" className="form-label me-2">Chat Server</label>
                        {isLoadingServers && <Spinner
                                    as="span"
                                    size="sm"
                                    role="status"
                                    aria-hidden="true"
                                    className="me-2" />}
                        {servers && 
                            <Input 
                                id="chatServer" 
                                name="select" 
                                type="select" 
                                value={selectedServerId} 
                                onChange={(e) => setSelectedServerId(e.target.value)}
                                disabled={!!props.serverId}
                            >
                                {/* Only show placeholder if there are multiple servers */}
                                {servers.length > 1 && <option value="">Select a chat server...</option>}
                                {servers.map((server) => (
                                    <option key={server.id} value={server.id}>{server.name} - {server.uri}</option>
                                ))}
                            </Input>}
                        {serversError && <div className="text-light rounded-1 py-2 px-3 m-1 bg-danger bg-opacity-75" aria-hidden="true">
                                    <span className="me-3">Failed to retrieve chat servers</span>
                                    <button disabled={isFetching || isLoading} type="button" className="btn btn-sm btn-secondary float-end" aria-label="Retry" onClick={() => refetch()}>Retry</button>
                                </div>}
                    </div>
                </div>
                <div className="d-inline-flex">
                    <div className="m-2">
                        <label htmlFor="model" className="d-block form-label me-2">Model</label>
                        {isLoading && <Spinner
                                    as="span"
                                    size="sm"
                                    role="status"
                                    aria-hidden="true"
                                    className="m-2" />}
                        {!selectedServerId && 
                            <div className="text-muted">Please select a chat server first</div>}
                        {selectedServerId && data && 
                            <Input id="model" name="select" type="select" value={model || ""} onChange={(e) => setModel(e.target.value)}>
                                {!model && <option value="">Select a model...</option>}
                                {data.sort((a, b) => a.name?.localeCompare(b.name ?? "") ?? 0).map((model) => (
                                    <option key={model.name} value={model.name}>{model.name} - {formatFileSize(model.size, 2)}</option>
                                ))}
                            </Input>}
                        {selectedServerId && (error || data?.length === 0) && <div className="text-light rounded-1 py-2 px-3 m-1 bg-danger bg-opacity-75" aria-hidden="true">
                                    <span className="me-3">Failed to retrieve models</span>
                                    <button disabled={isFetching || isLoading} type="button" className="btn btn-sm btn-secondary float-end" aria-label="Retry" onClick={() => refetch()}>Retry</button>
                                </div>}
                    </div>

                    <div className="m-2">
                        <label className="d-block form-label">MCP Tools</label>
                        {<div className="d-flex align-items-center">
                            {modelSupportsTools && <button 
                                type="button" 
                                className={"btn " + (selectedToolIds.length + selectedBagIds.length > 0 ? "btn-success" : "btn-secondary")}
                                onClick={() => setShowToolModal(true)}
                                disabled={!selectedServerId || !model || modelSupportsTools === null || isLoadingCapabilities}
                            >
                                <FontAwesomeIcon icon={faTools} className="me-2" />
                                Tools ({selectedToolIds.length + selectedBagIds.length} selections)
                                {isLoadingCapabilities && (
                                    <Spinner size="sm" className="ms-2" />
                                )}
                            </button>}
                            {!selectedServerId && (
                                <small className="text-muted m-1">Select a chat server first</small>
                            )}
                            {selectedServerId && (!model || isLoading) && (
                                <small className="text-muted m-1">Select a model first</small>
                            )}
                            {selectedServerId && model && isLoadingCapabilities && (
                                <small className="text-muted m-1">Checking model capabilities...</small>
                            )}
                            {selectedServerId && model && !isLoadingCapabilities && modelSupportsTools === false && (
                                <small className="text-muted m-1">Selected model does not support tools</small>
                            )}
                            {selectedServerId && capabilitiesError && <div className="text-light rounded-1 py-2 px-3 m-1 bg-danger bg-opacity-75" aria-hidden="true">
                                    <span className="me-3">Failed to retrieve model capabilities</span>
                                    <button disabled={isFetching || isLoading} type="button" className="btn btn-sm btn-secondary float-end" aria-label="Retry" onClick={() => refetch()}>Retry</button>
                                </div>}
                        </div>}
                    </div>
                </div>
                <div className="d-inline-flex col-md-12 flex-row">
                    <div className="d-inline-flex">
                        <div className="m-2">
                            <label htmlFor="temperature" className="form-label">Temperature</label>
                            <input required type="number" min={0} max={5} step={0.05} className="form-control" id="temperature" placeholder="0.123" value={temperature} onChange={(e) => setTemperature(parseFloat(e.target.value))} />
                        </div>
                    </div>
                    <div className="d-inline-flex">
                        <div className="m-2">
                            <label htmlFor="topP" className="form-label">TopP</label>
                            <input required type="number" min={0} max={5} step={0.05} className="form-control" id="topP" placeholder="0.123" value={topP} onChange={(e) => setTopP(parseFloat(e.target.value))} />
                        </div>
                    </div>
                    <div className="d-inline-flex">
                        <div className="m-2">
                            <label htmlFor="presencePenalty" className="form-label">Presence Penalty</label>
                            <input required type="number" min={0} max={5} step={0.05} className="form-control" id="presencePenalty" placeholder="0.123" value={presencePenalty} onChange={(e) => setPresencePenalty(parseFloat(e.target.value))} />
                        </div>
                    </div>
                    <div className="d-inline-flex">
                        <div className="m-2">
                            <label htmlFor="frequencyPenalty" className="form-label">Frequency Penalty</label>
                            <input required type="number" min={0} max={5} step={0.05} className="form-control" id="frequencyPenalty" placeholder="0.123" value={frequencyPenalty} onChange={(e) => setFrequencyPenalty(parseFloat(e.target.value))} />
                        </div>
                    </div>
                </div>
                <div className="d-inline-flex col-md-12 flex-row">
                </div>
                <div className="col-md-12 d-flex justify-content-end align-content-center">
                </div>
            </form>
            
            <ToolSelectionModal
                isOpen={showToolModal}
                onClose={() => setShowToolModal(false)}
                onConfirm={(toolIds, bagIds) => { setSelectedToolIds(toolIds); setSelectedBagIds(bagIds); }}
                initialSelectedToolIds={selectedToolIds}
                initialSelectedBagIds={selectedBagIds}
            />
        </div>
    );
};

export default ChatForm;
