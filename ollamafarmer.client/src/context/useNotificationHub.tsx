import { useContext } from 'react';
import { NotificationHubContext, type NotificationHubContextType } from './NotificationHubContext';

export const useNotificationHub = (): NotificationHubContextType => {
    const context = useContext(NotificationHubContext);
    if (context === undefined) {
        throw new Error('useNotificationHub must be used within a NotificationHubProvider');
    }
    return context;
};
