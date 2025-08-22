import ReactMarkdown from 'react-markdown';
import './ChatMessage.scss';
import { Spinner } from 'reactstrap';
import { memo } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCopy, faGear, faPencilAlt, faRobot, faTrash, faUser, faWrench } from '@fortawesome/free-solid-svg-icons';
import { toast } from 'react-toastify';

export interface ChatMessageProps {
    id?: string;
    name: string;
    message: string;
    index?: number;
    placeholder: boolean;
    hideThoughts?: boolean;
    imageUrl?: string;
    imageAlt?: string;
    onDelete?: (id: string) => void;
    onEdit?: (id: string) => void;
    toolCallId?: string | null;
    toolCallName?: string | null;
    // Use never-map to align with OpenAPI schema, but treated as unknown at runtime
    toolCallArgs?: Record<string, never> | undefined;
    isCompletedFunctionCall?: boolean;
    isCompletedToolResult?: boolean;
}

const parseMessage = (message: string, hideThoughts?: boolean): string => {
    if (!message) return "";
    const thinkRegex = /<think>([\s\S]*?)<\/think>/g;
    if (hideThoughts) return message.replace(thinkRegex, '> ---\n');
    return message.replace(thinkRegex, (_, p1: string) =>
        p1.split('\n').map(line => line.trim() ? '> ' + line.trim() : '').join('\n')
    );
};

const ChatMessage = memo(function ChatMessage(props: ChatMessageProps) {

    const isUser = props.name === "user";
    const isAssistant = props.name === "assistant";
    const isSystem = props.name === "system";
    const isTool = props.name === "tool";

    const handleCopy = async () => {
        try {
            await navigator.clipboard.writeText(props.message);
            toast.success("Message copied to clipboard");
        } catch {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = props.message;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            toast.success("Message copied to clipboard");
        }
    };

    // If this is a tool message and the content looks like JSON, render as a JSON block (with copy button)
    let parsedToolResult: unknown | null = null;
    if (isTool && props.message) {
        try {
            parsedToolResult = JSON.parse(props.message);
        } catch {
            parsedToolResult = null;
        }
    }

    const containerClass =
                     isUser ? "col-md-8 mx-auto me-2 me-md-5 border-primary bg-primary-010 text-light" :
                isAssistant ? "col-md-8 mx-auto ms-2 ms-md-5 border-secondary bg-secondary-010 text-light" :
         isSystem || isTool ? "w-50 mx-auto border-secondary bg-info-010 text-light" :
                    "";
                    
    return (
        props && props.name && (props.message || props.toolCallId || props.toolCallName || props.imageUrl) && (
            <div className="m-2 m-md-3 m-lg-4">
                <div className={"card d-flex rounded-3 text-center glow border-1 " + containerClass} aria-hidden="true">
                    <div className="card-body p-2 mb-0">
                        <div className="d-flex float-end justify-content-between align-items-center">
                            {/* <div className="d-flex align-items-center">
                                 {isUser && <FontAwesomeIcon icon={faArrowAltCircleDown} className="me-2" />}
                                {isAssistant && <FontAwesomeIcon icon={faTrashRestore} className="me-2" />}
                                {isSystem && <FontAwesomeIcon icon={faTrashArrowUp} className="me-2" />} 
                            </div> */}
                            {/* <span className="text-capitalize card-title font">{props.name}</span> */}
                            <div className="d-flex align-items-end msg-actions">
                                {props.onDelete && (
                                    <button type="button" className="btn btn-danger btn-sm m-1"
                                        onClick={() => props.onDelete && props.id && props.onDelete(props.id)}>
                                        <FontAwesomeIcon icon={faTrash} className="" />                                        
                                    </button>
                                )}
                                {props.onEdit && (
                                    <button type="button" className="btn btn-secondary btn-sm m-1"
                                        onClick={() => props.onEdit && props.id && props.onEdit(props.id)}>
                                        <FontAwesomeIcon icon={faPencilAlt} className="" />
                                        
                                    </button>
                                )}
                                <button type="button" className="btn btn-secondary btn-sm m-1"
                                    onClick={handleCopy}
                                    title="Copy message to clipboard">
                                    <FontAwesomeIcon icon={faCopy} className="" />
                                </button>
                            </div>
                        </div>
                        {/* <hr className="m-1" /> */}
                        <div className={"text-start " + (props.placeholder ? "placeholder-glow" : "")}>
                                {isUser && <div className="badge p-2 px-2 bg-primary"><FontAwesomeIcon className="me-1" icon={faUser} /> User</div>}
                                {isAssistant && <div className="badge p-2 px-2 bg-secondary"><FontAwesomeIcon className="me-1" icon={faRobot} /> Assistant</div>}
                                {isSystem && <div className="badge p-2 px-2 bg-info text-light"><FontAwesomeIcon className="me-1" icon={faGear} /> System</div>}
                                {isTool && <div className="badge p-2 px-2 bg-info text-light"><FontAwesomeIcon className="me-1" icon={faWrench} /> Tool</div>}
                        </div>
                        <div className="text-start p-2 msg-container">
                            {(props.toolCallId || props.toolCallName) && (
                                <div className="mb-2">
                                    <span className="badge bg-info me-2">Tool: {props.toolCallName || 'call'}</span>
                                    {/* Effective state: result trumps call state */}
                                    {props.isCompletedToolResult && (
                                        <span className="badge bg-success me-2">Completed</span>
                                    )}
                                    {!props.isCompletedToolResult && props.isCompletedFunctionCall && (
                                        <span className="badge bg-secondary me-2">Waiting for result</span>
                                    )}
                                    {!props.isCompletedToolResult && props.isCompletedFunctionCall === false && (
                                        <span className="badge bg-warning text-dark me-2">Pending call</span>
                                    )}
                                    {props.toolCallArgs && (
                                        <details className="mt-2">
                                            <summary className="small">Arguments</summary>
                                            <div className="position-relative">
                                                <button
                                                    type="button"
                                                    className="btn btn-sm btn-outline-secondary position-absolute end-0 mt-1 me-1"
                                                    title="Copy arguments JSON"
                                                    onClick={async (e) => {
                                                        e.preventDefault(); e.stopPropagation();
                                                        try {
                                                            await navigator.clipboard.writeText(JSON.stringify(props.toolCallArgs, null, 2));
                                                            toast.success('Arguments copied');
                                                        } catch {
                                                            toast.error('Failed to copy');
                                                        }
                                                    }}
                                                >
                                                    <FontAwesomeIcon icon={faCopy} />
                                                </button>
                                                <pre className="bg-dark text-light p-2 rounded-2 small overflow-auto" style={{ maxHeight: '200px' }}>
                                                    {JSON.stringify(props.toolCallArgs, null, 2)}
                                                </pre>
                                            </div>
                                        </details>
                                    )}
                                </div>
                            )}
                            {props.imageUrl 
                                && <div className="my-3 p-2"><img src={props.imageUrl} alt={props.imageAlt || "Avatar"} 
                                    className="img-fluid rounded-3" 
                                    style={{ maxHeight: "33%", maxWidth: "33%", cursor: "pointer" }}
                                    onClick={() => props.imageUrl && window.open(props.imageUrl, "_blank")}
                                    /></div>}
                            {/* Tool results: always show an expandable Result section; JSON pretty-printed, otherwise plain text */}
                            {!props.placeholder && isTool && props.message && (
                                <details className="mt-2">
                                    <summary className="small">Result</summary>
                                    <div className="position-relative">
                                        <button
                                            type="button"
                                            className="btn btn-sm btn-outline-secondary position-absolute end-0 mt-1 me-1"
                                            title={parsedToolResult !== null ? "Copy result JSON" : "Copy result text"}
                                            onClick={async (e) => {
                                                e.preventDefault(); e.stopPropagation();
                                                try {
                                                    const text = parsedToolResult !== null
                                                        ? JSON.stringify(parsedToolResult, null, 2)
                                                        : props.message;
                                                    await navigator.clipboard.writeText(text);
                                                    toast.success('Result copied');
                                                } catch {
                                                    toast.error('Failed to copy');
                                                }
                                            }}
                                        >
                                            <FontAwesomeIcon icon={faCopy} />
                                        </button>
                                        {parsedToolResult !== null ? (
                                            <pre className="bg-dark text-light p-2 rounded-2 small overflow-auto" style={{ maxHeight: '300px' }}>
                                                {JSON.stringify(parsedToolResult, null, 2)}
                                            </pre>
                                        ) : (
                                            <pre className="bg-dark text-light p-2 rounded-2 small overflow-auto" style={{ maxHeight: '300px' }}>
                                                {props.message}
                                            </pre>
                                        )}
                                    </div>
                                </details>
                            )}
                            {/* Non-tool messages: render markdown as before */}
                            {!props.placeholder && !isTool && (
                                <ReactMarkdown skipHtml={false}>{parseMessage(props.message, props.hideThoughts)}</ReactMarkdown>
                            )}
                            {props.placeholder &&
                                <p className="card-text placeholder-glow">
                                    <span className="placeholder col-2 mx-1"></span>
                                    <span className="placeholder col-4 mx-1"></span>
                                    <span className="placeholder col-6 mx-1"></span>
                                    <span className="placeholder col-1 mx-1"></span>
                                    <span className="placeholder col-5 mx-1"></span>
                                </p>}
                            {props.placeholder && <Spinner className="float-end" color="primary" size="sm"></Spinner>}
                        </div>
                        
                        <div className="mt-2 d-flex justify-content-end align-items-end">
                            {props.index !== undefined && <span className="text-muted small me-2">#{props.index}</span>}
                        </div>
                    </div>
                </div>
            </div>)
    );
});

export default ChatMessage;
