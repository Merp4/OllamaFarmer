
import './ChatMessage.css';
import { Spinner } from 'reactstrap';

export interface PlaceholderChatMessageProps {
    name: string;
    //message: string;
    //placeholder: boolean;
}


function PlaceholderChatMessage(props: PlaceholderChatMessageProps) {

    return (
        props && (
            <div className="mb-4 mx-2 msg-container">
                <div className="card d-flex text-start bg-light-subtle bot-message" aria-hidden="true">
                    <div className="card-body">
                        <h4 className="card-title text-capitalize">{props.name ?? "..."}</h4>
                        <hr />
                        <p className="card-text placeholder-glow">
                            <section>
                                    <p className="card-text placeholder-glow">
                                        <span className="placeholder col-7 mx-1"></span>
                                        <span className="placeholder col-4 mx-1"></span>
                                        <span className="placeholder col-4 mx-1"></span>
                                        <span className="placeholder col-6 mx-1"></span>
                                        <span className="placeholder col-8 mx-1"></span>
                                        <span className="placeholder col-3 mx-1"></span>
                                        <span className="placeholder col-5 mx-1"></span>
                                        </p>
                            </section>
                            <Spinner className="float-end" color="primary" size="sm"></Spinner>
                        </p>
                    </div>
                </div>
            </div>)
    );
}

export default PlaceholderChatMessage;
