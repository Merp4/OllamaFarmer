import React, { useEffect, useMemo, useState } from 'react';
import { HubConnection } from '@microsoft/signalr';
import { toast } from 'react-toastify';
import { createHubConnection, startHubConnection } from '../api/notificationHub';
import { NotificationHubContext } from './NotificationHubContext';

interface NotificationHubProviderProps {
    children: React.ReactNode;
}

export const NotificationHubProvider: React.FC<NotificationHubProviderProps> = ({ children }) => {
    
    const [connectionRef, setConnection] = useState<HubConnection | null>(null);

    useEffect(() => {
        setConnection(createHubConnection());
    }, []);
    
    useEffect(() => {
        if (connectionRef) {
            try {
                startHubConnection(connectionRef);
            } catch (error) {
                console.error(error as Error);
                toast.error(JSON.stringify(error));
            }
        }

        return () => {
            connectionRef?.stop();
        };
    }, [connectionRef]);
    
    const contextValue = useMemo(() => ({ connection: connectionRef }), [connectionRef]);

    return (
        <NotificationHubContext.Provider value={contextValue}>
            {children}
        </NotificationHubContext.Provider>
    );
};
export default NotificationHubProvider;
