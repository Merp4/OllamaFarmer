import { Button, Modal, ModalBody, ModalFooter, ModalHeader, Form, FormGroup, Input, Label } from "reactstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSave, faXmark } from '@fortawesome/free-solid-svg-icons';
import { useState, useEffect } from 'react';
import type { components } from "../../../api/schema";
import './EditModelModal.scss';

type ChatModelDto = components["schemas"]["ChatModelDto"];

interface EditModelModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave: (updatedModel: Partial<ChatModelDto>) => void;
    model: ChatModelDto | null;
}

function EditModelModal({ isOpen, onClose, onSave, model }: EditModelModalProps) {
    const [formData, setFormData] = useState<Partial<ChatModelDto>>({
        name: '',
        description: '',
        url: '',
        imageUrl: '',
        parameterSize: '',
        quantizationLevel: '',
        family: '',
        size: 0
    });

    // Thinking feature toggle (UI-only for now)
    const [enableThinking, setEnableThinking] = useState<boolean>(false);

    // Update form data when model changes
    useEffect(() => {
        if (model) {
            setFormData({
                name: model.name || '',
                description: model.description || '',
                url: model.url || '',
                imageUrl: model.imageUrl || '',
                parameterSize: model.parameterSize || '',
                quantizationLevel: model.quantizationLevel || '',
                family: model.family || '',
                size: model.size || 0
            });
            // Initialize thinking from model data
            setEnableThinking(model.enableThinking || false);
        }
    }, [model]);

    const handleInputChange = (field: keyof ChatModelDto, value: string | number) => {
        setFormData(prev => ({ ...prev, [field]: value }));
    };

    const handleSave = () => {
        if (!formData.name?.trim()) {
            // Simple validation - could be enhanced with a proper validation library
            return;
        }
        
        // Create update payload with only editable fields
        // Explicitly exclude critical fields that should not be modified via edit form
        const editableFields = {
            name: formData.name,
            description: formData.description,
            url: formData.url,
            imageUrl: formData.imageUrl,
            parameterSize: formData.parameterSize,
            quantizationLevel: formData.quantizationLevel,
            family: formData.family,
            size: formData.size
        };
        
        const updatedModel = {
            ...editableFields,
            enableThinking,
            // Preserve the original capabilities to prevent them from being overwritten
            capabilities: model?.capabilities || { 
                strings: [], 
                supportsCompletion: false, 
                supportsTools: false, 
                supportsVision: false 
            }
        };
        onSave(updatedModel);
        onClose();
    };

    const handleClose = () => {
        // Reset form data to original values
        if (model) {
            setFormData({
                name: model.name || '',
                description: model.description || '',
                url: model.url || '',
                imageUrl: model.imageUrl || '',
                parameterSize: model.parameterSize || '',
                quantizationLevel: model.quantizationLevel || '',
                family: model.family || '',
                size: model.size || 0
            });
            // Reset thinking toggle
            setEnableThinking(model.enableThinking || false);
        }
        onClose();
    };

    return (
        <Modal isOpen={isOpen} className="modal-lg edit-model-modal" backdrop={true} centered>
            <ModalHeader toggle={handleClose}>
                <FontAwesomeIcon icon={faSave} className="me-2" />
                Edit Model
            </ModalHeader>
            <ModalBody>
                <Form>
                    <FormGroup>
                        <Label for="modelName">Model Name *</Label>
                        <Input 
                            type="text" 
                            id="modelName" 
                            placeholder="Enter model name" 
                            value={formData.name || ''} 
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
                            value={formData.description || ''} 
                            onChange={(e) => handleInputChange('description', e.target.value)} 
                        />
                    </FormGroup>
                    
                    <FormGroup>
                        <Label for="modelUrl">URL</Label>
                        <Input 
                            type="url" 
                            id="modelUrl" 
                            placeholder="Enter model URL" 
                            value={formData.url || ''} 
                            onChange={(e) => handleInputChange('url', e.target.value)} 
                        />
                    </FormGroup>
                    
                    <FormGroup>
                        <Label for="modelImageUrl">Image URL</Label>
                        <Input 
                            type="url" 
                            id="modelImageUrl" 
                            placeholder="Enter image URL" 
                            value={formData.imageUrl || ''} 
                            onChange={(e) => handleInputChange('imageUrl', e.target.value)} 
                        />
                    </FormGroup>
                    
                    <div className="row">
                        <div className="col-md-3">
                            <FormGroup>
                                <Label for="parameterSize">Parameter Size</Label>
                                <Input 
                                    type="text" 
                                    id="parameterSize" 
                                    placeholder="e.g., 7B, 13B" 
                                    value={formData.parameterSize || ''} 
                                    onChange={(e) => handleInputChange('parameterSize', e.target.value)} 
                                />
                            </FormGroup>
                        </div>
                        <div className="col-md-3">
                            <FormGroup>
                                <Label for="quantizationLevel">Quantization Level</Label>
                                <Input 
                                    type="text" 
                                    id="quantizationLevel" 
                                    placeholder="e.g., Q4_0, Q8_0" 
                                    value={formData.quantizationLevel || ''} 
                                    onChange={(e) => handleInputChange('quantizationLevel', e.target.value)} 
                                />
                            </FormGroup>
                        </div>
                        <div className="col-md-3">
                            <FormGroup>
                                <Label for="family">Family</Label>
                                <Input 
                                    type="text" 
                                    id="family" 
                                    placeholder="e.g., llama, mistral" 
                                    value={formData.family || ''} 
                                    onChange={(e) => handleInputChange('family', e.target.value)} 
                                />
                            </FormGroup>
                        </div>
                        <div className="col-md-3">
                            <FormGroup>
                                <Label for="size">Size (bytes)</Label>
                                <Input 
                                    type="number" 
                                    id="size" 
                                    placeholder="Model size in bytes" 
                                    value={formData.size || ''} 
                                    onChange={(e) => handleInputChange('size', parseInt(e.target.value) || 0)} 
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
                </Form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={handleSave} disabled={!formData.name?.trim()}>
                    <FontAwesomeIcon icon={faSave} className="me-2" />
                    Save Changes
                </Button>
                <Button color="secondary" onClick={handleClose}>
                    <FontAwesomeIcon icon={faXmark} className="me-2" />
                    Cancel
                </Button>
            </ModalFooter>
        </Modal>
    );
}

export default EditModelModal;
