
//import './ChatMessageList.css';
import { ImageBaseUrl } from '../../../api/fileApi';
import type { components } from '../../../api/schema';
import ChatMessage from './ChatMessage';

export interface ChatMessageListProps {
    messages: (components["schemas"]["AppMessageDto"])[];
    placeholder: boolean;
    hideThoughts?: boolean;
    onDelete?: (id: string) => void;
    onEdit?: (id: string) => void;
}


function ChatMessageList(props: ChatMessageListProps) {


    return (
        props && props.messages && (            
            <div className="">          
                {props.messages?.sort((a, b) => (a.index ?? 0) - (b.index ?? 0)).filter((message) => message.content).map((message) => (
                    <ChatMessage
                        hideThoughts={props.hideThoughts}
                        key={message.id ?? message.index ?? -1}
                        id={message.id}
                        index={message.index}
                        name={"" + message.role}
                        message={"" + message.content}
                        placeholder={false}
                        imageUrl={message.images && (message.images.length >= 1) && ImageBaseUrl() + message.images?.[0] || ""} 
                        imageAlt={"Attached Image"}
                        onDelete={() => props.onDelete && message.id && props.onDelete(message.id)}
                        onEdit={() => props.onEdit && message.id && props.onEdit(message.id)}                        
                        />
                ))}
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
