//import { $api } from "../api/api";

import { faXmark } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { Button, Modal, ModalBody, ModalFooter, ModalHeader } from "reactstrap";
import { useState, useEffect } from "react";
import { FileType, ImageBaseUrl, type FileMetadata } from "../../api/fileApi";

export interface FilePreviewProps {
    file: FileMetadata | undefined;
}

export function FilePreview(props: FilePreviewProps) {
    const [textContent, setTextContent] = useState<string>("");
    const [textLoading, setTextLoading] = useState<boolean>(false);
    
    const file = props.file;

    // Load text content when file changes and is a text file
    useEffect(() => {
        if (file && file.type === FileType.Text) {
            setTextLoading(true);
            setTextContent("");
            
            const fileUrl = getFileUrl(file.path);
            fetch(fileUrl)
                .then(response => response.text())
                .then(content => {
                    setTextContent(content);
                    setTextLoading(false);
                })
                .catch(error => {
                    console.error("Failed to load text file:", error);
                    setTextContent("Error loading file content.");
                    setTextLoading(false);
                });
        }
    }, [file]);
    
    if (!file) {
        return (
            <div className="text-center p-4">
                <p>No file selected</p>
            </div>
        );
    }

    // Convert file path to a downloadable URL if needed
    const getFileUrl = (filePath: string) => {
        // If path is already a URL, return as is
        if (filePath.startsWith('http')) {
            return filePath;
        }
        // Otherwise, construct the download URL
        const encodedPath = encodeURIComponent(filePath);
        return `${ImageBaseUrl()}${encodedPath}`;
    };

    const renderFileContent = () => {
        const fileUrl = getFileUrl(file.path);
        
        switch (file.type) {
            case FileType.Image:
                return (
                    <img 
                        src={fileUrl} 
                        className="img-fluid" 
                        alt={file.name}
                        style={{ maxHeight: '70vh', objectFit: 'contain' }}
                    />
                );
            
            case FileType.Video:
                return (
                    <video controls className="img-fluid" style={{ maxHeight: '70vh', width: '100%' }}>
                        <source src={fileUrl} type="video/mp4" />
                        Your browser does not support the video tag.
                    </video>
                );
            
            case FileType.Audio:
                return (
                    <div className="text-center p-4">
                        <audio controls className="w-100">
                            <source src={fileUrl} type="audio/mpeg" />
                            Your browser does not support the audio element.
                        </audio>
                    </div>
                );
            
            case FileType.Text:
                return (
                    <div className="p-3 rounded border " style={{ maxHeight: '70vh', overflow: 'auto' }}>
                        <pre className="mb-0">
                            {textLoading ? "Loading file content..." : textContent || "No content available"}
                        </pre>
                    </div>
                );
            
            default:
                return (
                    <div className="text-center p-4">
                        <p>No preview available for this file type.</p>
                        <small className="text-muted">File: {file.name} (Type: {file.type})</small>
                    </div>
                );
        }
    };

    return (
        <div className="file-preview">
            <div className="mb-3">
                <h6 className="mb-1">{file.name}</h6>
                <small className="text-muted">{file.path}</small>
            </div>
            <div className="text-center">
                {renderFileContent()}
            </div>
        </div>
    );
}

export interface FilePreviewModalProps {
    file: FileMetadata | undefined;
    isOpen: boolean;
    toggle: () => void;
}

export function FilePreviewModal(props: FilePreviewModalProps) {
    return (
        <Modal 
            isOpen={props.isOpen} 
            toggle={props.toggle}
            className="modal-fullscreen-lg-down"
            size="lg"
            backdrop={true}
            scrollable
            unmountOnClose
            returnFocusAfterClose
            centered
        >
            <ModalHeader toggle={props.toggle}>
                File Preview
                {props.file && (
                    <small className="ms-2 text-muted">- {props.file.name}</small>
                )}
            </ModalHeader>
            <ModalBody>
                <FilePreview file={props.file} />
            </ModalBody>
            <ModalFooter>
                <Button color="secondary" onClick={props.toggle}>
                    <FontAwesomeIcon icon={faXmark} className="me-2" />
                    Close
                </Button>
            </ModalFooter>
        </Modal>
    );
}

