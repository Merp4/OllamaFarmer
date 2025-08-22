
//import './ChatMessageList.css';
import { ImageBaseUrl } from '../../../api/fileApi';
import type { components } from '../../../api/schema';
import ChatMessage from './ChatMessage';

type AppMessageWithTools = components["schemas"]["AppMessageDto"] & {
    toolCallId?: string | null;
    toolCallName?: string | null;
    toolCallArgs?: Record<string, never> | null;
    isCompletedFunctionCall?: boolean;
    isCompletedToolResult?: boolean;
};

export interface ChatMessageListProps {
    messages: AppMessageWithTools[];
    placeholder: boolean;
    hideThoughts?: boolean;
    onDelete?: (id: string) => void;
    onEdit?: (id: string) => void;
}


function ChatMessageList(props: ChatMessageListProps) {


    return (
        props && props.messages && (            
            <div className="">          
                {props.messages
                    ?.sort((a, b) => (a.index ?? 0) - (b.index ?? 0))
                    // Show messages that have text, images, are tool results, or carry tool-call metadata
                    .filter((message) =>
                        Boolean(message.content)
                        || (Array.isArray(message.images) && message.images.length > 0)
                        || message.role === 'tool'
                        || Boolean(message.toolCallId || message.toolCallName)
                    )
                    .map((message) => {
                        const displayRole = (String(message.role) === 'assistant'
                            && (message.toolCallId || message.toolCallName)
                            && !message.content)
                            ? 'tool'
                            : String(message.role);

                        // If this is a function-call-only assistant message, derive completion from the existence of a tool result with the same callId
                        const derivedToolCompleted = Boolean(message.isCompletedToolResult)
                            || (!!message.toolCallId && props.messages.some(m2 => (
                                m2.toolCallId === message.toolCallId
                                && (String(m2.role) === 'tool' || Boolean(m2.isCompletedToolResult))
                            )));

                        return (
                    <ChatMessage
                        hideThoughts={props.hideThoughts}
                        key={message.id ?? message.index ?? -1}
                        id={message.id}
                        index={message.index}
                        name={displayRole}
                        message={(message.content ?? "") + ""}
                        placeholder={false}
                        imageUrl={message.images && (message.images.length >= 1) && ImageBaseUrl() + message.images?.[0] || ""} 
                        imageAlt={"Attached Image"}
                        toolCallId={message.toolCallId as string | undefined}
                        toolCallName={message.toolCallName as string | undefined}
                        toolCallArgs={message.toolCallArgs as Record<string, never> | undefined}
                        isCompletedFunctionCall={message.isCompletedFunctionCall as boolean | undefined}
                        isCompletedToolResult={(displayRole === 'tool') ? derivedToolCompleted : (message.isCompletedToolResult as boolean | undefined)}
                        onDelete={() => props.onDelete && message.id && props.onDelete(message.id)}
                        onEdit={() => props.onEdit && message.id && props.onEdit(message.id)}                        
                        />
                );})}
                {props.placeholder && (
                    <ChatMessage
                        key={-1}
                        name={"assistant"}
                        message="Awaiting reply"
                        placeholder={true}
                        />)}
                        {props.messages?.length === 0 && (
                            <div key={-1} className="d-flex flex-column flex-fill m-5" aria-hidden="true">
                                    <div className="d-flex flex-column flex-fill m-5 text-center">
                                        <p className="my-5">No messages.</p>
                                </div>
                            </div>
                        )}
            </div>)
    );
}

export default ChatMessageList;
