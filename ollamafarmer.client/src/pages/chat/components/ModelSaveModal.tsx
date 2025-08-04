import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from "reactstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFloppyDisk, faXmark } from '@fortawesome/free-solid-svg-icons';

interface ModelSaveModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave: () => void;
    modelName: string;
    setModelName: (name: string) => void;
}

function ModelSaveModal({ isOpen, onClose, onSave, modelName, setModelName }: ModelSaveModalProps) {
    return (
        <Modal isOpen={isOpen} className="modal-lg" fullscreen={false} backdrop={true} centered>
            <ModalHeader>Save to new Model</ModalHeader>
            <ModalBody>
                <form className="d-inline-flex flex-column">
                    <div className="m-2">
                        <label htmlFor="modelName" className="form-label">Model Name</label>
                        <input 
                            required 
                            type="text" 
                            className="form-control" 
                            id="modelName" 
                            placeholder="Enter model name" 
                            value={modelName} 
                            onChange={(e) => setModelName(e.target.value)} 
                        />
                    </div>
                </form>
            </ModalBody>
            <ModalFooter>
                <Button color="primary" onClick={onSave}>
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

export default ModelSaveModal;
