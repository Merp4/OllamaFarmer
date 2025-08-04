import { useState, useEffect } from "react";
import { Modal, ModalHeader, ModalBody, ModalFooter, Button, FormGroup, Input, Label, Alert, Accordion, AccordionHeader, AccordionItem, AccordionBody } from "reactstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCheck, faTimes, faTools, faServer } from "@fortawesome/free-solid-svg-icons";
import { $fetchClient } from "../api/api";
import type { components } from "../api/schema";
import "./ToolSelectionModal.scss";

type McpServer = components["schemas"]["McpServer"];
type McpTool = components["schemas"]["McpTool"];

interface ToolSelectionModalProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: (selectedToolIds: string[]) => void;
    initialSelectedToolIds?: string[];
}

interface ServerToolsGroup {
    server: McpServer;
    tools: McpTool[];
    selectedToolIds: Set<string>;
}

export function ToolSelectionModal({ isOpen, onClose, onConfirm, initialSelectedToolIds = [] }: ToolSelectionModalProps) {
    const [serverGroups, setServerGroups] = useState<ServerToolsGroup[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // Fetch tools for all servers when modal opens
    useEffect(() => {
        if (!isOpen) {
            setServerGroups([]);
            return;
        }

        const fetchAllServersAndTools = async () => {
            setLoading(true);
            setError(null);
            
            try {
                // First fetch all MCP servers
                const serversResponse = await $fetchClient.GET("/api/ToolServer/all");
                
                if (!serversResponse.data || serversResponse.data.length === 0) {
                    setServerGroups([]);
                    return;
                }

                const groups: ServerToolsGroup[] = [];
                
                // Then fetch tools for each server
                for (const server of serversResponse.data) {
                    if (!server.id) continue;
                    
                    try {
                        const toolsResponse = await $fetchClient.GET("/api/ToolServer/{serverId}/tools", {
                            params: { path: { serverId: server.id } }
                        });
                        
                        if (toolsResponse.data) {
                            groups.push({
                                server,
                                tools: toolsResponse.data,
                                selectedToolIds: new Set(
                                    toolsResponse.data
                                        .filter((tool: McpTool) => tool.id && initialSelectedToolIds.includes(tool.id))
                                        .map((tool: McpTool) => tool.id!)
                                )
                            });
                        }
                    } catch (serverError) {
                        console.error(`Failed to fetch tools for server ${server.name}:`, serverError);
                        // Add server with empty tools list if fetch fails
                        groups.push({
                            server,
                            tools: [],
                            selectedToolIds: new Set()
                        });
                    }
                }
                
                setServerGroups(groups);
            } catch (err) {
                console.error("Failed to fetch servers and tools:", err);
                setError("Failed to load servers and tools. Please try again.");
            } finally {
                setLoading(false);
            }
        };

        fetchAllServersAndTools();
    }, [isOpen, initialSelectedToolIds]);

    const handleToolToggle = (serverIndex: number, toolId: string) => {
        setServerGroups(prev => prev.map((group, index) => {
            if (index === serverIndex) {
                const newSelected = new Set(group.selectedToolIds);
                if (newSelected.has(toolId)) {
                    newSelected.delete(toolId);
                } else {
                    newSelected.add(toolId);
                }
                return { ...group, selectedToolIds: newSelected };
            }
            return group;
        }));
    };

    const handleServerToggle = (serverIndex: number, selectAll: boolean) => {
        setServerGroups(prev => prev.map((group, index) => {
            if (index === serverIndex) {
                const newSelected = selectAll 
                    ? new Set(group.tools.map(tool => tool.id!).filter(Boolean))
                    : new Set<string>();
                return { ...group, selectedToolIds: newSelected };
            }
            return group;
        }));
    };

    const handleConfirm = () => {
        const allSelectedToolIds = serverGroups.flatMap(group => 
            Array.from(group.selectedToolIds)
        );
        onConfirm(allSelectedToolIds);
        onClose();
    };

    const totalSelectedTools = serverGroups.reduce((sum, group) => sum + group.selectedToolIds.size, 0);

    // Accordion open state
    const [openAccordion, setOpenAccordion] = useState<string>("0");

    const handleAccordionToggle = (targetId: string) => {
        setOpenAccordion(prevOpen => (prevOpen === targetId ? "" : targetId));
    };

    if (!isOpen) return null;

    return (
        <Modal isOpen={isOpen} toggle={onClose} size="lg" 
            className="modal-fullscreen-lg-down" backdrop="static">
            <ModalHeader toggle={onClose}>
                <FontAwesomeIcon icon={faTools} className="me-2" />
                Select MCP Tools
            </ModalHeader>
            <ModalBody>
                {error && (
                    <Alert color="danger">
                        {error}
                    </Alert>
                )}

                {loading ? (
                    <div className="text-center p-4">
                        <div className="spinner-border" role="status">
                            <span className="visually-hidden">Loading tools...</span>
                        </div>
                        <p className="mt-2">Loading available tools...</p>
                    </div>
                ) : serverGroups.length === 0 ? (
                    <Alert color="info">
                        No MCP servers or tools available. Configure MCP servers in the Tools section to enable tool usage in chats.
                    </Alert>
                ) : (
                    <div className="oe-shadow">
                        <div className="mb-3">
                            <small className="text-muted">
                                Select which MCP tools should be available for this chat. 
                                Tools from different servers can be combined.
                            </small>
                        </div>
                        <Accordion flush className="mb-3 custom-accordion" open={openAccordion} toggle={handleAccordionToggle}>
                            {serverGroups.map((group, serverIndex) => (
                                <AccordionItem key={group.server.id} className="">
                                    <AccordionHeader 
                                        targetId={String(serverIndex)} 
                                        className="custom-accordion-header"
                                    >
                                        <div className="d-flex justify-content-between align-items-center w-100">
                                            <div>
                                                <FontAwesomeIcon icon={faServer} className="me-2" />
                                                {group.server.name} <span className="text-muted small">({group.tools.length} tools - {group.selectedToolIds.size} selected)</span>
                                            </div>
                                        </div>
                                    </AccordionHeader>
                                    <AccordionBody accordionId={String(serverIndex)} className="">
                                        
                                        <div className="">


                                            {group.server.description && (
                                                <div className="mb-2 w-auto">
                                                    <p className="text-muted small mb-3">{group.server.description}</p>
                                                </div>
                                            )}
                                            <div className="justify-content-end mb-3">
                                                <Button
                                                    size="sm"
                                                    color="primary"
                                                    className="me-2"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleServerToggle(serverIndex, true);
                                                    }}
                                                >
                                                    Select All
                                                </Button>
                                                <Button
                                                    size="sm"
                                                    color="secondary"
                                                    className="me-4"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        handleServerToggle(serverIndex, false);
                                                    }}
                                                >
                                                    Clear All
                                                </Button>
                                            </div>
                                        </div>

                                        {group.tools.length === 0 ? (
                                            <div className="text-muted text-center py-3">
                                                No tools available on this server
                                            </div>
                                        ) : (
                                            <div className="row">
                                                {group.tools.map(tool => (
                                                    <div key={tool.id} className="col-md-6 mb-2">
                                                        <FormGroup check>
                                                            <Input
                                                                type="checkbox"
                                                                id={`tool-${tool.id}`}
                                                                checked={tool.id ? group.selectedToolIds.has(tool.id) : false}
                                                                onChange={() => tool.id && handleToolToggle(serverIndex, tool.id)}
                                                            />
                                                            <Label check htmlFor={`tool-${tool.id}`} className="ms-2">
                                                                <strong>{tool.name}</strong>
                                                                {tool.description && (
                                                                    <div className="text-muted small">
                                                                        {tool.description}
                                                                    </div>
                                                                )}
                                                            </Label>
                                                        </FormGroup>
                                                    </div>
                                                ))}
                                            </div>
                                        )}
                                    </AccordionBody>
                                </AccordionItem>
                            ))}
                        </Accordion>
                    </div>
                )}
            </ModalBody>
            <ModalFooter>
                <div className="me-auto">
                    <small className="text-muted">
                        {totalSelectedTools} tool{totalSelectedTools !== 1 ? 's' : ''} selected
                    </small>
                </div>
                <Button color="secondary" onClick={onClose}>
                    <FontAwesomeIcon icon={faTimes} className="me-2" />
                    Cancel
                </Button>
                <Button 
                    color="primary" 
                    onClick={handleConfirm}
                    disabled={loading}
                >
                    <FontAwesomeIcon icon={faCheck} className="me-2" />
                    Confirm Selection
                </Button>
            </ModalFooter>
        </Modal>
    );
}
