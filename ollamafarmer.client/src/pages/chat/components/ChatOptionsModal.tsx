import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from "reactstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFloppyDisk, faXmark } from '@fortawesome/free-solid-svg-icons';
import { useState, useEffect } from 'react';

interface ChatOptions {
    temperature: number;
    topP: number;
    frequencyPenalty: number;
    presencePenalty: number;
}

interface ChatOptionsModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave: (options: ChatOptions) => void;
    initialOptions?: ChatOptions;
}

function ChatOptionsModal({ isOpen, onClose, onSave, initialOptions }: ChatOptionsModalProps) {
    const [options, setOptions] = useState<ChatOptions>({
        temperature: 0.7,
        topP: 0.3,
        frequencyPenalty: 0.5,
        presencePenalty: 0.1
    });

    useEffect(() => {
        if (initialOptions) {
            setOptions(initialOptions);
        }
    }, [initialOptions]);

    const handleSave = () => {
        onSave(options);
        onClose();
    };

    const handleFieldChange = (field: keyof ChatOptions, value: number) => {
        setOptions(prev => ({ ...prev, [field]: value }));
    };

    return (
        <Modal isOpen={isOpen} className="modal-lg" fullscreen={false} backdrop={true} centered>
            <ModalHeader>Chat Options</ModalHeader>
            <ModalBody>
                <form className="d-inline-flex flex-column">
                    <div className="m-2">
                        <label htmlFor="temperature" className="form-label">
                            Temperature ({options.temperature})
                        </label>
                        <input
                            type="range"
                            className="form-range"
                            id="temperature"
                            min="0"
                            max="2"
                            step="0.1"
                            value={options.temperature}
                            onChange={(e) => handleFieldChange('temperature', parseFloat(e.target.value))}
                        />
                        <div className="form-text">
                            Controls randomness. Lower values make responses more focused and deterministic.
                        </div>
                    </div>

                    <div className="m-2">
                        <label htmlFor="topP" className="form-label">
                            Top P ({options.topP})
                        </label>
                        <input
                            type="range"
                            className="form-range"
                            id="topP"
                            min="0"
                            max="1"
                            step="0.05"
                            value={options.topP}
                            onChange={(e) => handleFieldChange('topP', parseFloat(e.target.value))}
                        />
                        <div className="form-text">
                            Nucleus sampling. Alternative to temperature for controlling randomness.
                        </div>
                    </div>

                    <div className="m-2">
                        <label htmlFor="frequencyPenalty" className="form-label">
                            Frequency Penalty ({options.frequencyPenalty})
                        </label>
                        <input
                            type="range"
                            className="form-range"
                            id="frequencyPenalty"
                            min="0"
                            max="2"
                            step="0.1"
                            value={options.frequencyPenalty}
                            onChange={(e) => handleFieldChange('frequencyPenalty', parseFloat(e.target.value))}
                        />
                        <div className="form-text">
                            Reduces repetition of frequently used words.
                        </div>
                    </div>

                    <div className="m-2">
                        <label htmlFor="presencePenalty" className="form-label">
                            Presence Penalty ({options.presencePenalty})
                        </label>
                        <input
                            type="range"
                            className="form-range"
                            id="presencePenalty"
                            min="0"
                            max="2"
                            step="0.1"
                            value={options.presencePenalty}
                            onChange={(e) => handleFieldChange('presencePenalty', parseFloat(e.target.value))}
                        />
                        <div className="form-text">
                            Encourages model to talk about new topics.
                        </div>
                    </div>
                </form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={handleSave}>
                    <FontAwesomeIcon icon={faFloppyDisk} className="me-2 small" />
                    Save
                </Button>
                <Button color="secondary" onClick={onClose}>
                    <FontAwesomeIcon icon={faXmark} className="me-2 small" />
                    Cancel
                </Button>
            </ModalFooter>
        </Modal>
    );
}

export default ChatOptionsModal;
