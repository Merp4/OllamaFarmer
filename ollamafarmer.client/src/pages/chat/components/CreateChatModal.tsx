import type { components } from "../../../api/schema";
import { useRef } from "react";
import { Modal, ModalHeader, ModalBody, ModalFooter, Button } from "reactstrap";
import ChatForm, { type CreateChatHandles } from "../components/CreateChat";
import { toast } from "react-toastify";
import { $queryClient } from "../../../api/api";
import { useNavigate } from "react-router";
import { faPlus, faXmark } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

type CreateChatRequest = components["schemas"]["CreateChatDto"];

interface CreateChatModalProps {
    onSuccess?: (id: string) => void;
    onError?: (error: unknown) => void;
    onSubmitted?: (chat: CreateChatRequest) => Promise<void>;
    redirectOnCreated?: boolean;
    isOpen?: boolean;
    toggle?: () => void;
    model?: string;
    serverId?: string;
}

function CreateChatModal({ onSuccess, onError, onSubmitted, isOpen, toggle, redirectOnCreated, model, serverId }: CreateChatModalProps) {

    const createChatRef = useRef<CreateChatHandles | null>(null);
    const navigate = useNavigate();

    const { mutateAsync: mutateCreateChatAsync } = $queryClient.useMutation("post", "/api/AppChat", {
        onMutate: (val) => {
            toast.info("Creating chat...");
            onSubmitted?.(val.body);
        },
        onSuccess: (resp) => {
            if (!resp || !resp.id) {
                toast.error("Chat creation failed: No ID returned");
                return;
            }

            toast.success("Chat created");
            toggle?.();
            onSuccess?.(resp.id);
            if(redirectOnCreated === undefined || !!redirectOnCreated) {
                navigate("/chat/" + resp.id);
            }
        },
        onError: (error) => {
            toast.error("Error creating chat: " + JSON.stringify(error, [], 2));
            onError?.(error);
        }
    });

    const onCreateChat = async (chat: CreateChatRequest) => {
        if (!chat) {
            return;
        }

        await mutateCreateChatAsync({ body: chat });
    }
    
    return (
        <Modal isOpen={isOpen} toggle={toggle} {...{ className: "modal-lg", fullscreen: false, backdrop: true }} static scrollable unmountOnClose returnFocusAfterClose centered>
            <ModalHeader toggle={toggle}>Create Chat</ModalHeader>
            <ModalBody>
                <ChatForm model={model} serverId={serverId} parentRef={createChatRef} onSubmitted={async (chat: CreateChatRequest) => { onCreateChat(chat); }} />
            </ModalBody>
            <ModalFooter>
                    <Button color="primary" onClick={() => createChatRef.current?.submitForm()}>
                        <FontAwesomeIcon icon={faPlus} className="me-2 small" />
                        Create
                    </Button>{' '}
                    <Button color="secondary" onClick={toggle}>
                        <FontAwesomeIcon icon={faXmark} className="me-2 small" />
                        Cancel
                    </Button>
            </ModalFooter>
        </Modal>
    );
}


export default CreateChatModal;
