import { type RefObject, useState, useCallback, memo } from "react";
import { Button, Input, InputGroup, InputGroupText, Modal, ModalBody, ModalFooter, ModalHeader, Spinner } from "reactstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUser, faRobot, faGears, faPaperclip, faPlus, faEnvelope, faExpand, faCompress, faEdit } from '@fortawesome/free-solid-svg-icons';
import Files from "../../files/FileExplorer";
import type { FileMetadata } from "../../../api/fileApi";

interface MessageInputProps {
  message: string;
  setMessage: (msg: string) => void;
  role: "user" | "assistant" | "system" | "tool";
  setRole: (role: "user" | "assistant" | "system" | "tool") => void;
  isBusy: boolean;
  isPendingAdd: boolean;
  awaitingReply: boolean;
  enableUpload?: boolean;
  onAddMessage: () => void;
  onSendMessage: () => void;
  onAttachFile?: (file: FileMetadata) => void;
  messageInputRef: RefObject<HTMLInputElement | null>;
}

const MessageInput = memo(function MessageInput({
    message, setMessage, role, setRole, isBusy, isPendingAdd, awaitingReply,
    onAddMessage, onSendMessage, messageInputRef, onAttachFile, enableUpload = true
    }: MessageInputProps) {

  const [selectedFile, setSelectedFile] = useState<FileMetadata | null>(null);
  const [fileSelectModalOpen, setFileSelectModalOpen] = useState(false);
  const [messageEditorModalOpen, setMessageEditorModalOpen] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const [modalMessage, setModalMessage] = useState("");

  const togglefileSelectModal = useCallback(() => setFileSelectModalOpen(prev => !prev), []);
  const handleFileClick = useCallback((file: FileMetadata) => {
    setSelectedFile(file);
    if (onAttachFile) 
        onAttachFile(file);
    setFileSelectModalOpen(false);
  }, [onAttachFile]);

  const toggleMessageEditor = useCallback(() => {
    setModalMessage(message);
    setMessageEditorModalOpen(prev => !prev);
  }, [message]);

  const toggleExpanded = useCallback(() => {
    setIsExpanded(prev => !prev);
  }, []);

  const saveModalMessage = useCallback(() => {
    setMessage(modalMessage);
    setMessageEditorModalOpen(false);
  }, [modalMessage, setMessage]);


  return (
    <div>
    <form
      className="d-block w-100 flex-column align-items-end d-flex shadow"
      onSubmit={(e) => {
        e.preventDefault();
        if (!isBusy && message)
          onSendMessage();
      }}
    >
      <InputGroup>
        <InputGroupText className="m-0 p-1 px-2">
          {role === "user" && <FontAwesomeIcon icon={faUser} className="me-2 ms-3 small" />}
          {role === "assistant" && <FontAwesomeIcon icon={faRobot} className="me-2 ms-3 small" />}
          {role === "system" && <FontAwesomeIcon icon={faGears} className="me-2 ms-3 small" />}
          <Input id="model" name="roleSelect" type="select"
            value={role}
            onChange={(e) => setRole((e.target.value as "user" | "assistant" | "system") ?? "user")}
            className="p-1">
            <option value="user">User</option>
            <option value="assistant">Assistant</option>
            <option value="system">System</option>
          </Input>
        </InputGroupText>
        <Button
          type="button"
          color={selectedFile ? "primary" : "secondary"}
          className={"d-flex align-items-center " + (enableUpload ? "" : "d-none")}
          onClick={togglefileSelectModal}
          disabled={isBusy}
        >
          <span>
            <FontAwesomeIcon icon={faPaperclip} className="small" />
          </span>
          <span
            className="d-none d-md-inline ms-2 text-truncate"
            style={{ maxWidth: "120px", display: "inline-block" }}
            title={selectedFile?.name}
          >
            {selectedFile?.name ?? "Attach"}
          </span>
        </Button>
        <Button type="button" color="secondary" className="shadow-none" onClick={toggleMessageEditor} disabled={isBusy}>
          <FontAwesomeIcon icon={faEdit} className="small" />
        </Button>
        <Button
          type="button"
          color="secondary"
            className="shadow-none"
          onClick={toggleExpanded}
          disabled={isBusy}
          title={isExpanded ? "Collapse" : "Expand"}
        >
          <FontAwesomeIcon icon={isExpanded ? faCompress : faExpand} className="small" />
        </Button>
        <Input
          id="message"
          name="message"
          innerRef={messageInputRef}
          value={message}
          autoComplete="off"
          autoFocus
          type="textarea"
          className="message-main-input"
          onChange={(e) => setMessage(e.target.value)}
          rows={isExpanded ? 5 : 1}
          onKeyDown={(e) => {
            if (!isBusy && message && e.key === "Enter" && !e.ctrlKey && !e.shiftKey) {
              e.preventDefault();
              onSendMessage();
            }
          }}
          placeholder="Type your message here..." />
        <Button type="button" color="secondary" onClick={onAddMessage} disabled={isBusy || !message}>
          {isPendingAdd && (<Spinner as="span" size="sm" role="status" aria-hidden="true" className="me-2" />)}
          {!isPendingAdd && <span><FontAwesomeIcon icon={faPlus} className="small" /></span>}
          <span className="d-none d-md-inline ms-2">Add</span>
        </Button>
        <Button type="submit" color="primary" onClick={onSendMessage} disabled={isBusy || !message}>
          {awaitingReply && (
            <Spinner as="span" size="sm" role="status" aria-hidden="true" className="me-2" />
          )}
          {!awaitingReply && (
            <span><FontAwesomeIcon icon={faEnvelope} className="" /></span>
          )}
          <span className="d-none d-md-inline ms-2">Send</span>
        </Button>
      </InputGroup>
    </form>
    
    {/* Message Editor Modal */}
    {messageEditorModalOpen && (
      <Modal isOpen={messageEditorModalOpen} toggle={toggleMessageEditor} centered={true} size="lg">
        <ModalHeader toggle={toggleMessageEditor}>Message Editor</ModalHeader>
        <ModalBody>
          <div className="mb-3">
            <label htmlFor="modalRole" className="form-label">Role</label>
            <Input 
              id="modalRole" 
              name="select" 
              type="select"
              value={role}
              onChange={(e) => setRole((e.target.value as "user" | "assistant" | "system") ?? "user")}
            >
              <option value="user">User</option>
              <option value="assistant">Assistant</option>
              <option value="system">System</option>
            </Input>
          </div>
          <div className="mb-3">
            <label htmlFor="modalMessage" className="form-label">Message</label>
            <Input
              id="modalMessage"
              name="modalMessage"
              value={modalMessage}
              type="textarea"
              rows={8}
              className="form-control"
              onChange={(e) => setModalMessage(e.target.value)}
              placeholder="Type your message here..."
              autoFocus
            />
          </div>
        </ModalBody>
        <ModalFooter>
          <Button color="primary" onClick={saveModalMessage}>
            Save
          </Button>
          <Button color="secondary" onClick={toggleMessageEditor}>
            Cancel
          </Button>
        </ModalFooter>
      </Modal>
    )}

    {/* File Selection Modal */}
    {fileSelectModalOpen && <Modal isOpen={fileSelectModalOpen} toggle={togglefileSelectModal} centered={true} size="lg">
      <ModalHeader toggle={togglefileSelectModal}>Attach Files</ModalHeader>
      <ModalBody>
        <Files
            initialDirectory="/"
            //onFilter={(file: FileMetadata) => !file.isDirectory && file.type == FileType.Image}
            onFileClick={handleFileClick}
        />
      </ModalBody>
      <ModalFooter>
        <Button color="secondary" onClick={() => { setSelectedFile(null); setFileSelectModalOpen(false); }}>None</Button>
        <Button color="secondary" onClick={togglefileSelectModal}>Cancel</Button>
      </ModalFooter>
    </Modal>}
  </div>
  );
});

export default MessageInput;
