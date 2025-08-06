import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import { ClientNotificationType, type ClientNotification } from "../api/notificationHub";
import { useNotificationHub } from "../context/useNotificationHub";

function SignalRPanel() {


    const { connection } = useNotificationHub();

    useEffect(() => {
        if (connection) {
            // Chat message handler ?
            const handleMessage = (msg: ClientNotification) => {
                toast(msg.message, { type: msg.type });
            };

            connection.on('chat', handleMessage);

            return () => {
                connection.off('chat', handleMessage);
            };
        }
    }, [connection]);

    const [message, setMessage] = useState<string>("");
    const [toastType, setToastType] = useState<ClientNotificationType>(ClientNotificationType.Default);

    const [rawMessage, setRawMessage] = useState<string>("");

    // function to handle sending a raw message
    const onBroadcastMessage = () => {
        if (connection) {
            connection.invoke('BroadcastRawNotification', rawMessage)
                .catch(() => {
                    toast.error("Failed to send message.");
                });
        }
    };

    // function to handle sending a toast notification
    const onBroadcastToast = () => {
        if (connection) {
            connection.invoke('BroadcastNotification', toastType, message)
                .catch(() => {
                    toast.error("Failed to send toast.");
                });
        }
    };

    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid mb-5">
                <h1 className="display-5 d-block">SignalR</h1>
                <p className="p-1">This component demonstrates SignalR functionality.</p>
            </div>
            <div className="container-fluid">
                <div className="container-fluid mb-5">
                    <h5>Send a Raw Message</h5>
                    <p className="p-1">Type text to send as a raw message.</p>
                    <div className="input-group mb-3">
                        <input type="text" className="form-control" placeholder="Type your message here..." value={rawMessage} onChange={e => setRawMessage(e.target.value)} />
                        <button className="btn btn-primary" type="button" onClick={onBroadcastMessage}>Broadcast</button>
                    </div>
                </div>


                <div className="container-fluid mb-5">
                    <h5>Send a Toast</h5>
                    <p className="p-1">Select a toast type and type your message to send a toast notification.</p>
                    <div className="input-group mb-3">
                        <select className="form-select" id="toastTypeSelect" value={toastType} onChange={e => setToastType(e.target.value as ClientNotification["type"])}>
                            <option value="default">Default</option>
                            <option value="info">Info</option>
                            <option value="success">Success</option>
                            <option value="warning">Warning</option>
                            <option value="error">Error</option>
                        </select>
                    </div>
                    <div className="input-group mb-3">
                        <input type="text" className="form-control" placeholder="Type your message here..." value={message} onChange={e => setMessage(e.target.value)} />
                        <button className="btn btn-primary" type="button" onClick={onBroadcastToast}>Broadcast</button>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default SignalRPanel;
