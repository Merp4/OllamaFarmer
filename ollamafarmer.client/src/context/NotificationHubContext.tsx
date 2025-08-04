import { createContext } from 'react';
import { HubConnection } from '@microsoft/signalr';

export interface NotificationHubContextType {
    connection: HubConnection | null;
}

export const NotificationHubContext = createContext<NotificationHubContextType | undefined>(undefined);
