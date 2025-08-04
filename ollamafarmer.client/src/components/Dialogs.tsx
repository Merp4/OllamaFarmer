// File for various dialogs used in the application
// ok dialog, text input dialog, yes/no dialog

import { Button, Modal, ModalHeader, ModalBody, ModalFooter, Input } from 'reactstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCheck, faTimes } from '@fortawesome/free-solid-svg-icons';

import './Dialogs.scss';

export interface OkDialogProps {
    isOpen: boolean;
    title: string;
    message: string;
    onClose: () => void;
    color?: string;
    titleColor?: string;
    titleBgColor?: string;
}

function OkDialog({ isOpen, title, message, onClose, color = "primary", titleColor, titleBgColor }: OkDialogProps) {
    const headerClassNames = 
    [
        titleColor ? `text-${titleColor}` : "text-body",
        "bg-" + (titleBgColor ?? "primary" )
    ]

    return (
        <Modal centered isOpen={isOpen} toggle={onClose} className="ok-dialog">
            <ModalHeader toggle={onClose} className={headerClassNames.join(" ") + " bg-opacity-50"}>{title}</ModalHeader>
            <ModalBody><p>{message}</p></ModalBody>
            <ModalFooter>
                <Button color={color} onClick={onClose}>
                    <FontAwesomeIcon icon={faCheck} /> OK
                </Button>
            </ModalFooter>
        </Modal>
    );
};

export interface TextInputDialogProps {
    isOpen: boolean;
    title: string;
    message: string;
    value: string;
    onChange: (value: string) => void;
    onSubmit: () => void;
    onCancel: () => void;
    color?: string;
    titleColor?: string;
    titleBgColor?: string;
}

function TextInputDialog({ isOpen, title, message, value, onChange, onSubmit, onCancel, color = "primary", titleColor, titleBgColor }: TextInputDialogProps) {
    const headerClassNames = 
    [
        titleColor ? `text-${titleColor}` : "text-body",
        "bg-" + (titleBgColor ?? "primary" )
    ]
    
    return (
        <Modal centered isOpen={isOpen} toggle={onCancel} className="text-input-dialog">
            <ModalHeader toggle={onCancel} className={headerClassNames.join(" ") + " bg-opacity-50"}>{title}</ModalHeader>
            <ModalBody>
                <p>{message}</p>
                <Input type="text" value={value} onChange={(e) => onChange(e.target.value)} />
            </ModalBody>
            <ModalFooter>
                <Button color={color} onClick={onSubmit}>
                    <FontAwesomeIcon icon={faCheck} /> Submit
                </Button>
                <Button color="secondary" onClick={onCancel}>
                    <FontAwesomeIcon icon={faTimes} /> Cancel
                </Button>
            </ModalFooter>
        </Modal>
    );
};

export interface YesNoDialogProps {
    isOpen: boolean;
    title: string;
    message: string;
    onYes: () => void;
    onNo: () => void;
    color?: string;
    titleColor?: string;
    titleBgColor?: string;
}

function YesNoDialog({ isOpen, title, message, onYes, onNo, color = "primary", titleColor, titleBgColor }: YesNoDialogProps) {
    const headerClassNames = 
    [
        titleColor ? `text-${titleColor}` : "text-body",
        "bg-" + (titleBgColor ?? "primary" )
    ]

    return (
        <Modal centered isOpen={isOpen} toggle={onNo} className="yes-no-dialog">
            <ModalHeader toggle={onNo} className={headerClassNames.join(" ") + " bg-opacity-50"}>{title}</ModalHeader>
            <ModalBody className=""><p>{message}</p></ModalBody>
            <ModalFooter className="">
                <Button color={color} onClick={onYes}>
                    <FontAwesomeIcon icon={faCheck} /> Yes
                </Button>
                <Button color="secondary" onClick={onNo}>
                    <FontAwesomeIcon icon={faTimes} /> No
                </Button>
            </ModalFooter>
        </Modal>
    );
};

export { OkDialog, TextInputDialog, YesNoDialog };
export default { OkDialog, TextInputDialog, YesNoDialog };
