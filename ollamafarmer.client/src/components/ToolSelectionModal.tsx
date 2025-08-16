import { useState, useEffect } from "react";
import { Modal, ModalHeader, ModalBody, ModalFooter, Button, FormGroup, Input, Label, Alert, Accordion, AccordionHeader, AccordionItem, AccordionBody } from "reactstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCheck, faTimes, faTools, faServer, faFolder } from "@fortawesome/free-solid-svg-icons";
import { $fetchClient, ApiBaseUrl } from "../api/api";
import type { components } from "../api/schema";
import "./ToolSelectionModal.scss";

type McpServer = components["schemas"]["McpServer"];
type McpTool = components["schemas"]["McpTool"];

type ToolBagDto = {
    id: string;
    name: string;
    description?: string | null;
    createdAt?: string;
    updatedAt?: string;
    toolIds: string[];
};

interface ToolSelectionModalProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: (selectedToolIds: string[], selectedBagIds: string[]) => void;
    initialSelectedToolIds?: string[];
    initialSelectedBagIds?: string[];
}

interface ServerToolsGroup {
    server: McpServer;
    tools: McpTool[];
    selectedToolIds: Set<string>;
}

export function ToolSelectionModal({ isOpen, onClose, onConfirm, initialSelectedToolIds = [], initialSelectedBagIds = [] }: ToolSelectionModalProps) {
    const [serverGroups, setServerGroups] = useState<ServerToolsGroup[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // ToolBags state
    const [toolBags, setToolBags] = useState<ToolBagDto[]>([]);
    const [selectedBagIds, setSelectedBagIds] = useState<Set<string>>(new Set(initialSelectedBagIds));

    // Fetch tools and toolbags when modal opens
    useEffect(() => {
        if (!isOpen) {
            setServerGroups([]);
            setToolBags([]);
            setSelectedBagIds(new Set());
            return;
        }

        const fetchAll = async () => {
            setLoading(true);
            setError(null);
            try {
                // Load ToolBags first
                try {
                    const bagsResp = await fetch(`${ApiBaseUrl()}/api/ToolBags`);
                    if (bagsResp.ok) {
                        const bags = await bagsResp.json() as ToolBagDto[];
                        setToolBags(bags);
                        setSelectedBagIds(new Set(initialSelectedBagIds));
                    } else {
                        setToolBags([]);
                    }
                } catch (e) {
                    console.error("Failed to load toolbags", e);
                    setToolBags([]);
                }

                // Then fetch all MCP servers
                const serversResponse = await $fetchClient.GET("/api/ToolServer/all");
                const groups: ServerToolsGroup[] = [];

                if (serversResponse.data && serversResponse.data.length > 0) {
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
                            groups.push({ server, tools: [], selectedToolIds: new Set() });
                        }
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

        fetchAll();
    }, [isOpen, initialSelectedToolIds, initialSelectedBagIds]);

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

    const handleBagToggle = (bagId: string) => {
        setSelectedBagIds(prev => {
            const next = new Set(prev);
            if (next.has(bagId)) next.delete(bagId); else next.add(bagId);
            return next;
        });
    };

    const handleConfirm = () => {
        const allSelectedToolIds = serverGroups.flatMap(group => Array.from(group.selectedToolIds));
        onConfirm(allSelectedToolIds, Array.from(selectedBagIds));
        onClose();
    };

    const totalSelectedTools = serverGroups.reduce((sum, group) => sum + group.selectedToolIds.size, 0);
    const totalSelectedBagTools = toolBags.reduce((sum, bag) => sum + (selectedBagIds.has(bag.id) ? (bag.toolIds?.length ?? 0) : 0), 0);

    // Accordion open state (servers)
    const [openAccordion, setOpenAccordion] = useState<string>("0");
    const handleAccordionToggle = (targetId: string) => {
        setOpenAccordion(prevOpen => (prevOpen === targetId ? "" : targetId));
    };

    // Accordion open state (toolbags)
    const [openBagsAccordion, setOpenBagsAccordion] = useState<string>("");
    const handleBagsAccordionToggle = (targetId: string) => {
        setOpenBagsAccordion(prevOpen => (prevOpen === targetId ? "" : targetId));
    };

    if (!isOpen) return null;

    return (
        <Modal isOpen={isOpen} toggle={onClose} size="lg" scrollable={true}
            backdrop="static">
            <ModalHeader toggle={onClose}>
                <FontAwesomeIcon icon={faTools} className="me-2" />
                Select Tools and ToolBags
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
                ) : (serverGroups.length === 0 && toolBags.length === 0) ? (
                    <Alert color="info">
                        No MCP servers, tools, or ToolBags available. Configure MCP servers and ToolBags to enable tool usage in chats.
                    </Alert>
                ) : (
                    <div className="oe-shadow">
                        <div className="mb-3">
                            <small className="text-muted">
                                Select which MCP tools and ToolBags should be available for this chat.
                            </small>
                        </div>

                        {/* MCP Servers accordion - unchanged styling/behavior */}
                        {serverGroups.length > 0 && (
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
                        )}

                        {/* ToolBags accordion - new, separate section */}
                        {toolBags.length > 0 && (
                            <Accordion flush className="mb-3 custom-accordion" open={openBagsAccordion} toggle={handleBagsAccordionToggle}>
                                <AccordionItem>
                                    <AccordionHeader targetId="bags" className="custom-accordion-header">
                                        <div className="d-flex justify-content-between align-items-center w-100">
                                            <div>
                                                <FontAwesomeIcon icon={faFolder} className="me-2" />
                                                ToolBags <span className="text-muted small">({toolBags.length} bags - {selectedBagIds.size} selected)</span>
                                            </div>
                                        </div>
                                    </AccordionHeader>
                                    <AccordionBody accordionId="bags">
                                        <div className="justify-content-end mb-3">
                                            <Button size="sm" color="primary" className="me-2" onClick={() => setSelectedBagIds(new Set(toolBags.map(b => b.id)))}>Select All</Button>
                                            <Button size="sm" color="secondary" className="me-4" onClick={() => setSelectedBagIds(new Set())}>Clear All</Button>
                                        </div>

                                        <div className="row">
                                            {toolBags.map(bag => (
                                                <div key={bag.id} className="col-md-6 mb-2">
                                                    <FormGroup check>
                                                        <Input type="checkbox" id={`bag-${bag.id}`} checked={selectedBagIds.has(bag.id)} onChange={() => handleBagToggle(bag.id)} />
                                                        <Label check htmlFor={`bag-${bag.id}`} className="ms-2">
                                                            <strong>{bag.name}</strong>
                                                            <span className={"badge ms-2 " + ((bag.toolIds?.length || 0) > 0 ? "bg-primary" : "bg-warning")}>{bag.toolIds?.length ?? 0}</span>
                                                            <div className="text-muted small">{bag.description || "No description"}</div>
                                                        </Label>
                                                    </FormGroup>
                                                </div>
                                            ))}
                                        </div>
                                    </AccordionBody>
                                </AccordionItem>
                            </Accordion>
                        )}
                    </div>
                )}
            </ModalBody>
            <ModalFooter>
                <div className="me-auto">
                    <small className="text-muted">
                        {totalSelectedTools} tool{totalSelectedTools !== 1 ? 's' : ''} selected • {selectedBagIds.size} bag{selectedBagIds.size !== 1 ? 's' : ''} selected • {totalSelectedBagTools} tool{totalSelectedBagTools !== 1 ? 's' : ''} in bags
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
