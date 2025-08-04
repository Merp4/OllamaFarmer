import { useContext } from 'react';
import { DialogContext } from '../context/DialogContextDefinition';
import type { UseDialogsReturn } from './useDialogs';

export function useDialogContext(): UseDialogsReturn {
    const context = useContext(DialogContext);
    if (!context) {
        throw new Error('useDialogContext must be used within a DialogContextProvider');
    }
    return context;
}
