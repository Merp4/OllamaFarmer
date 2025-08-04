import { useState, useEffect } from "react";
import { faSave, faTimes } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import type { components } from "../api/schema";

type McpServer = components["schemas"]["McpServer"];
type ChatServerDto = components["schemas"]["ChatServerDto"];

interface ServerModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave: (server: McpServer | ChatServerDto) => Promise<void>;
    server?: McpServer | ChatServerDto | null;
    serverType: "mcp" | "chat";
    title: string;
}

export function ServerModal({ isOpen, onClose, onSave, server, serverType, title }: ServerModalProps) {
    const [formData, setFormData] = useState({
        id: server?.id || "",
        name: server?.name || "",
        description: server?.description || "",
        version: server?.version || "",
        uri: server?.uri ? String(server.uri) : ""
    });
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [errors, setErrors] = useState<Record<string, string>>({});

    // Update form data when server prop changes
    useEffect(() => {
        setFormData({
            id: server?.id || "",
            name: server?.name || "",
            description: server?.description || "",
            version: server?.version || "",
            uri: server?.uri ? String(server.uri) : ""
        });
        setErrors({}); // Clear any existing errors when switching servers
    }, [server]);

    const validateForm = () => {
        const newErrors: Record<string, string> = {};
        
        if (!formData.name.trim()) {
            newErrors.name = "Name is required";
        }
        
        if (!formData.uri.trim()) {
            newErrors.uri = "URI is required";
        } else {
            try {
                new URL(formData.uri);
            } catch {
                newErrors.uri = "Invalid URI format";
            }
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        
        if (!validateForm()) {
            return;
        }

        setIsSubmitting(true);
        try {
            const serverData = {
                ...formData,
                id: formData.id || undefined,
                uri: formData.uri,
                createdAt: server?.createdAt || new Date().toISOString(),
                updatedAt: new Date().toISOString()
            };

            if (serverType === "mcp") {
                await onSave(serverData as McpServer);
            } else {
                await onSave(serverData as ChatServerDto);
            }
            
            onClose();
        } catch (error) {
            console.error("Failed to save server:", error);
            setErrors({ general: "Failed to save server. Please try again." });
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleChange = (field: string, value: string) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        // Clear field error when user starts typing
        if (errors[field]) {
            setErrors(prev => ({ ...prev, [field]: "" }));
        }
    };

    if (!isOpen) return null;

    return (
        <div 
            className="modal fade show" 
            style={{ 
                display: "block", 
                backgroundColor: "rgba(0, 0, 0, 0.5)" 
            }} 
            tabIndex={-1} 
            onClick={onClose}
        >
            <div className="modal-dialog modal-lg modal-dialog-centered" onClick={(e) => e.stopPropagation()}>
                <div className="modal-content">
                    <div className="modal-header">
                        <h5 className="modal-title">{title}</h5>
                        <button type="button" className="btn-close" onClick={onClose}></button>
                    </div>
                    <form onSubmit={handleSubmit}>
                        <div className="modal-body">
                            {errors.general && (
                                <div className="alert alert-danger" role="alert">
                                    {errors.general}
                                </div>
                            )}
                            
                            <div className="mb-3">
                                <label htmlFor="serverName" className="form-label">Name *</label>
                                <input
                                    type="text"
                                    className={`form-control ${errors.name ? "is-invalid" : ""}`}
                                    id="serverName"
                                    value={formData.name}
                                    onChange={(e) => handleChange("name", e.target.value)}
                                    disabled={isSubmitting}
                                />
                                {errors.name && <div className="invalid-feedback">{errors.name}</div>}
                            </div>

                            <div className="mb-3">
                                <label htmlFor="serverDescription" className="form-label">Description</label>
                                <textarea
                                    className="form-control"
                                    id="serverDescription"
                                    rows={3}
                                    value={formData.description}
                                    onChange={(e) => handleChange("description", e.target.value)}
                                    disabled={isSubmitting}
                                />
                            </div>

                            <div className="mb-3">
                                <label htmlFor="serverUri" className="form-label">URI *</label>
                                <input
                                    type="url"
                                    className={`form-control ${errors.uri ? "is-invalid" : ""}`}
                                    id="serverUri"
                                    value={formData.uri}
                                    onChange={(e) => handleChange("uri", e.target.value)}
                                    placeholder="https://example.com/api"
                                    disabled={isSubmitting}
                                />
                                {errors.uri && <div className="invalid-feedback">{errors.uri}</div>}
                            </div>

                            <div className="mb-3">
                                <label htmlFor="serverVersion" className="form-label">Version</label>
                                <input
                                    type="text"
                                    className="form-control"
                                    id="serverVersion"
                                    value={formData.version}
                                    onChange={(e) => handleChange("version", e.target.value)}
                                    placeholder="1.0.0"
                                    disabled={isSubmitting}
                                />
                            </div>
                        </div>
                        <div className="modal-footer">
                            <button type="button" className="btn btn-secondary" onClick={onClose} disabled={isSubmitting}>
                                <FontAwesomeIcon icon={faTimes} className="me-2" />
                                Cancel
                            </button>
                            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                                <FontAwesomeIcon icon={faSave} className="me-2" />
                                {isSubmitting ? "Saving..." : "Save"}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
}
