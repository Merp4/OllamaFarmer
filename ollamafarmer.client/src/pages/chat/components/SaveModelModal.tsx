import { Button, Modal, ModalBody, ModalFooter, ModalHeader, Form, FormGroup, Input, Label } from "reactstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSave, faXmark } from '@fortawesome/free-solid-svg-icons';
import { useState, useEffect } from 'react';
import type { components } from "../../../api/schema";
import { $queryClient } from "../../../api/api";
import LoadingPanel from "../../../components/LoadingPanel";
import './SaveModelModal.scss';

type AppChat = components["schemas"]["AppChatDto"];
type ChatRole = components["schemas"]["NullableOfChatRole"];

interface SaveModelModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave: (modelData: {
        name: string;
        description?: string;
        url?: string;
        imageUrl?: string;
        parameterSize?: string;
        quantizationLevel?: string;
        family?: string;
        enableThinking: boolean;
    }) => void;
    chatData?: AppChat;
    initialModelName?: string;
}

// Helper function to extract system message
function getSystemMessage(messages?: { role?: ChatRole; content?: string | null; images?: string[] | null }[]): string {
    if (!messages || messages.length === 0) return "";
    const firstSystemMessage = messages[0];
    if (firstSystemMessage.role !== "system") return "";
    return firstSystemMessage.content ?? "";
}

function SaveModelModal({ isOpen, onClose, onSave, chatData, initialModelName }: SaveModelModalProps) {
    const [formData, setFormData] = useState({
        name: '',
        description: '',
        url: '',
        imageUrl: '',
        parameterSize: '',
        quantizationLevel: '',
        family: '',
    });

    const [enableThinking, setEnableThinking] = useState<boolean>(false);

    // Fetch model details when chat data is available and modal is open
    const { data: modelDetails, isLoading: isLoadingModel } = $queryClient.useQuery(
        "get",
        "/api/ChatModel/all",
        { params: { query: {} } },
        {
            enabled: isOpen && !!chatData?.model,
            refetchOnWindowFocus: false,
            select: (data) => {
                // Find the model that matches the chat's model name
                return data?.find(model => model.name === chatData?.model);
            }
        }
    );

    // Update form data when modal opens or chat data changes
    useEffect(() => {
        if (isOpen) {
            const systemMessage = getSystemMessage(chatData?.messages);
            const baseModelName = chatData?.model || 'Unknown Model';
            
            setFormData({
                name: initialModelName || chatData?.name || `${baseModelName} - ${new Date().toLocaleDateString()}`,
                description: systemMessage || `Model based on ${baseModelName} chat session`,
                url: modelDetails?.url || '',
                imageUrl: modelDetails?.imageUrl || '',
                parameterSize: modelDetails?.parameterSize || '',
                quantizationLevel: modelDetails?.quantizationLevel || '',
                family: modelDetails?.family || baseModelName.split(':')[0] || '', // Use model family or extract from model name
            });
            setEnableThinking(false);
        }
    }, [isOpen, chatData, initialModelName, modelDetails]);

    const handleInputChange = (field: keyof typeof formData, value: string) => {
        setFormData(prev => ({ ...prev, [field]: value }));
    };

    const handleSave = () => {
        if (!formData.name?.trim()) {
            return;
        }
        
        onSave({
            ...formData,
            enableThinking
        });
    };

    const handleClose = () => {
        onClose();
    };

    return (
        <Modal 
            isOpen={isOpen} 
            size="lg"
            backdrop={true} 
            scrollable={true}
            centered={true}
            fade={true}
        >
            <ModalHeader toggle={handleClose}>
                <FontAwesomeIcon icon={faSave} className="me-2" />
                Save Chat as Model
            </ModalHeader>
            <ModalBody>
                {isLoadingModel ? (
                    <LoadingPanel />
                ) : (
                    <Form>
                    <FormGroup>
                        <Label for="modelName">Model Name *</Label>
                        <Input 
                            type="text" 
                            id="modelName" 
                            placeholder="Enter model name" 
                            value={formData.name} 
                            onChange={(e) => handleInputChange('name', e.target.value)}
                            required
                            invalid={!formData.name?.trim()}
                        />
                    </FormGroup>
                    
                    <FormGroup>
                        <Label for="modelDescription">Description</Label>
                        <Input 
                            type="textarea" 
                            id="modelDescription" 
                            placeholder="Enter model description" 
                            rows={3}
                            value={formData.description} 
                            onChange={(e) => handleInputChange('description', e.target.value)} 
                        />
                        <div className="form-text">
                            Description will default to the system message from the chat if not provided.
                        </div>
                    </FormGroup>
                    
                    <FormGroup>
                        <Label for="modelUrl">URL</Label>
                        <Input 
                            type="url" 
                            id="modelUrl" 
                            placeholder="Enter model URL (optional)" 
                            value={formData.url} 
                            onChange={(e) => handleInputChange('url', e.target.value)} 
                        />
                    </FormGroup>
                    
                    <FormGroup>
                        <Label for="modelImageUrl">Image URL</Label>
                        <Input 
                            type="url" 
                            id="modelImageUrl" 
                            placeholder="Enter image URL (optional)" 
                            value={formData.imageUrl} 
                            onChange={(e) => handleInputChange('imageUrl', e.target.value)} 
                        />
                    </FormGroup>
                    
                    <div className="row">
                        <div className="col-md-4">
                            <FormGroup>
                                <Label for="parameterSize">Parameter Size</Label>
                                <Input 
                                    type="text" 
                                    id="parameterSize" 
                                    placeholder="e.g., 7B, 13B, 70B" 
                                    value={formData.parameterSize} 
                                    onChange={(e) => handleInputChange('parameterSize', e.target.value)} 
                                />
                            </FormGroup>
                        </div>
                        <div className="col-md-4">
                            <FormGroup>
                                <Label for="quantizationLevel">Quantization</Label>
                                <Input 
                                    type="text" 
                                    id="quantizationLevel" 
                                    placeholder="e.g., Q4_K_M, Q8_0" 
                                    value={formData.quantizationLevel} 
                                    onChange={(e) => handleInputChange('quantizationLevel', e.target.value)} 
                                />
                            </FormGroup>
                        </div>
                        <div className="col-md-4">
                            <FormGroup>
                                <Label for="family">Model Family</Label>
                                <Input 
                                    type="text" 
                                    id="family" 
                                    placeholder="e.g., llama, qwen, gemma" 
                                    value={formData.family} 
                                    onChange={(e) => handleInputChange('family', e.target.value)} 
                                />
                            </FormGroup>
                        </div>
                    </div>
                    
                    {/* Thinking Feature Toggle */}
                    <FormGroup check>
                        <Input 
                            type="checkbox" 
                            id="enableThinking" 
                            checked={enableThinking}
                            onChange={(e) => setEnableThinking(e.target.checked)}
                        />
                        <Label check for="enableThinking" className="ms-2">
                            <strong>Enable Thinking Mode</strong>
                            <div className="text-muted small">
                                When enabled, this model will show a "Thoughts" button in the chat interface to display reasoning process
                            </div>
                        </Label>
                    </FormGroup>

                    {/* Chat Information */}
                    {chatData && (
                        <div className="mt-4 p-3 border-light rounded">
                            <h6>Chat Information</h6>
                            <div className="row small">
                                <div className="col-md-6">
                                    <strong>Base Model:</strong> {chatData.model || 'Unknown'}
                                </div>
                                <div className="col-md-6">
                                    <strong>Messages:</strong> {chatData.messages?.length || 0}
                                </div>
                                {chatData.options && (
                                    <div className="col-12 mt-2">
                                        <strong>Settings:</strong> 
                                        {chatData.options.temperature !== undefined && ` Temperature: ${chatData.options.temperature}`}
                                        {chatData.options.topP !== undefined && `, Top-P: ${chatData.options.topP}`}
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </Form>
                )}
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={handleSave} disabled={!formData.name?.trim() || isLoadingModel}>
                    <FontAwesomeIcon icon={faSave} className="me-2" />
                    Save Model
                </Button>
                <Button color="secondary" onClick={handleClose}>
                    <FontAwesomeIcon icon={faXmark} className="me-2" />
                    Cancel
                </Button>
            </ModalFooter>
        </Modal>
    );
}

export default SaveModelModal;
