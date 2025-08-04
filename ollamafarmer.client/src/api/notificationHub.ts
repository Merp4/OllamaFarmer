import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { toast } from "react-toastify";


const NotificationHubUrl = () => import.meta.env.VITE_SIGNALR_HUBURL_NOTIFICATIONS;

export interface ClientNotification {
    connectionId?: string;
    userName?: string;
    userId?: string;
    message: string;
    timestamp: Date;
    type: ClientNotificationType;
};

export enum ClientNotificationType {
    Default = "default",
    Info = "info",
    Success = "success",
    Warning = "warning",
    Error = "error",
}

export const createHubConnection = (): HubConnection => {
    return new HubConnectionBuilder()
        .withUrl(NotificationHubUrl(), {
            //withCredentials: false,

// https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0
            
            accessTokenFactory: () => {
                const token = localStorage.getItem("accessToken");
                if (!token) {
                    console.warn("Access token is not available in localStorage.");
                    return "";
                }
                return token;
            },
            headers: {
                "Authorization": `Bearer ${localStorage.getItem("accessToken") || "aaaaaaaaaaaaaaaaaaa"}`,
            },
        })
        .withAutomaticReconnect()
        .build();
};

export const startHubConnection = (connectionRef: HubConnection, ) => {
    return connectionRef
        .start()
        .then(() => {
            connectionRef.on('notification', (msg: ClientNotification) => {
                console.log('SignalRmessage', msg);
                toast(msg.message, { type: msg.type });
            });
            connectionRef.onreconnecting((error) => {
                console.warn('Connection lost, attempting to reconnect...', error);
                toast.error('Connection lost, attempting to reconnect...', {
                    autoClose: 5000
                });
            });
            connectionRef.onreconnected((connectionId) => {
                console.log('Reconnected to SignalR hub with connection ID:', connectionId);
                toast.success('Reconnected to SignalR hub', {
                    autoClose: 5000
                });
            });
        })
        .catch((err) => {
            //logger.error(`Error: ${err}`);
            console.error(err);
            toast.error(err);
        });
};

// export const useHubConnection = () => {
//     const [connectionRef, setConnection] = useState<HubConnection | null>(null);

//     useEffect(() => {
//         createHubConnection(setConnection);
//     }, []);
    
//     useEffect(() => {
//         if (connectionRef) {
//             try {
//                 startHubConnection(connectionRef);
//             } catch (error) {
//                 console.error(error as Error);
//                 toast.error(JSON.stringify(error));
//             }
//         }

//         return () => {
//             connectionRef?.stop();
//         };
//     }, [connectionRef]);
    
//     return connectionRef;
// }

// example cust hook to create a hub connection
// https://github.com/TanStack/query/blob/main/packages/react-query/src/QueryClientProvider.tsx

// export const createHubConnection2222 = () => {
//     const connectionRef = new HubConnectionBuilder()
//         .withUrl(NotificationHubUrl, {
//             accessTokenFactory: () => {
//                 return localStorage.getItem("accessToken") || "";
//             },
//         })
//         .configureLogging(LogLevel.Information)
//         .build();

//     return connectionRef;
// };
