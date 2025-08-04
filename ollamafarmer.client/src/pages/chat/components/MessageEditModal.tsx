import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from "reactstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFloppyDisk, faXmark } from '@fortawesome/free-solid-svg-icons';
import type { components } from '../../../api/schema';

type ChatRole = components["schemas"]["NullableOfChatRole"];
type ChatMessage = components["schemas"]["AppMessageDto"];

interface MessageEditModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSave: () => void;
    messageToEdit?: ChatMessage;
    setMessageToEdit: (message?: ChatMessage) => void;
}

function MessageEditModal({ isOpen, onClose, onSave, messageToEdit, setMessageToEdit }: MessageEditModalProps) {
    return (
        <Modal isOpen={isOpen} className="modal-lg" fullscreen={false} backdrop={true} centered>
            <ModalHeader>Edit Message</ModalHeader>
            <ModalBody>
                <form className="d-inline-flex flex-column">
                    <div className="m-2">
                        <label htmlFor="messageContent" className="form-label">Message Content</label>
                        <textarea 
                            required 
                            className="form-control" 
                            id="messageContent" 
                            rows={3} 
                            placeholder="Enter message content" 
                            value={messageToEdit?.content || ""} 
                            onChange={(e) => setMessageToEdit({ ...messageToEdit, content: e.target.value })}
                        ></textarea>
                    </div>
                    <div className="m-2">
                        <label htmlFor="messageRole" className="form-label">Message Role</label>
                        <select
                            className="form-select"
                            id="messageRole"
                            value={typeof messageToEdit?.role === "string" ? messageToEdit.role : "user"}
                            onChange={(e) => setMessageToEdit({ ...messageToEdit, role: e.target.value as ChatRole })}
                        >
                            <option value="user">User</option>
                            <option value="assistant">Assistant</option>
                            <option value="system">System</option>
                            <option value="tool">Tool</option>
                        </select>
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

export default MessageEditModal;
