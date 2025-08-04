import { createContext } from 'react';
import type { UseDialogsReturn } from '../hooks/useDialogs';

export const DialogContext = createContext<UseDialogsReturn | undefined>(undefined);
