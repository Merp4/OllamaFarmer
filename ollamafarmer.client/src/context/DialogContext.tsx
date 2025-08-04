import React from 'react';
import { useDialogs } from '../hooks/useDialogs';
import { DialogProvider } from '../components/DialogProvider';
import { DialogContext } from './DialogContextDefinition';

export function DialogContextProvider({ children }: { children: React.ReactNode }) {
    const dialogs = useDialogs();

    return (
        <DialogContext.Provider value={dialogs}>
            <DialogProvider dialogs={dialogs}>
                {children}
            </DialogProvider>
        </DialogContext.Provider>
    );
}

export default DialogContextProvider;
