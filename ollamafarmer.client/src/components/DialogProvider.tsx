import React, { useState } from 'react';
import { TextInputDialog, YesNoDialog, OkDialog } from './Dialogs';
import type { UseDialogsReturn } from '../hooks/useDialogs';

export interface DialogProviderProps {
    children: React.ReactNode;
    dialogs: UseDialogsReturn;
}

export function DialogProvider({ children, dialogs }: DialogProviderProps) {
    const [textInputValue, setTextInputValue] = useState('');

    // Update text input value when dialog opens
    React.useEffect(() => {
        if (dialogs.textInputDialog.isOpen) {
            setTextInputValue(dialogs.textInputDialog.defaultValue || '');
        }
    }, [dialogs.textInputDialog.isOpen, dialogs.textInputDialog.defaultValue]);

    return (
        <>
            {children}
            
            {/* Text Input Dialog */}
            {dialogs.textInputDialog && <TextInputDialog
                isOpen={dialogs.textInputDialog.isOpen}
                title={dialogs.textInputDialog.title}
                message={dialogs.textInputDialog.message}
                value={textInputValue}
                onChange={setTextInputValue}
                color={dialogs.textInputDialog.color}
                titleColor={dialogs.textInputDialog.titleColor}
                titleBgColor={dialogs.textInputDialog.titleBgColor}
                onSubmit={() => {
                    dialogs.textInputDialog.onConfirm?.(textInputValue);
                }}
                onCancel={() => {
                    dialogs.textInputDialog.onCancel?.();
                }}
            />}

            {/* Yes/No Dialog */}
            {dialogs.yesNoDialog && <YesNoDialog
                isOpen={dialogs.yesNoDialog.isOpen}
                title={dialogs.yesNoDialog.title}
                message={dialogs.yesNoDialog.message}
                color={dialogs.yesNoDialog.color}
                titleColor={dialogs.yesNoDialog.titleColor}
                titleBgColor={dialogs.yesNoDialog.titleBgColor}
                onYes={() => {
                    dialogs.yesNoDialog.onConfirm?.();
                }}
                onNo={() => {
                    dialogs.yesNoDialog.onCancel?.();
                }}
            />}

            {/* OK Dialog */}
            {dialogs.okDialog && <OkDialog
                isOpen={dialogs.okDialog.isOpen}
                title={dialogs.okDialog.title}
                message={dialogs.okDialog.message}
                color={dialogs.okDialog.color}
                titleColor={dialogs.okDialog.titleColor}
                titleBgColor={dialogs.okDialog.titleBgColor}
                onClose={() => {
                    dialogs.okDialog.onConfirm?.();
                }}
            />}
        </>
    );
}

export default DialogProvider;
