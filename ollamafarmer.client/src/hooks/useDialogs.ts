import { useState, useCallback } from 'react';

export interface DialogState {
    isOpen: boolean;
    title: string;
    message: string;
    defaultValue?: string;
    onConfirm?: (value?: string) => void;
    onCancel?: () => void;
    color?: string;
    titleColor?: string;
    titleBgColor?: string;
}

export interface UseDialogsReturn {
    // Text input dialog
    textInputDialog: DialogState;
    showTextInputDialog: (title: string, message: string, defaultValue: string, onConfirm: (value: string) => void, color?: string, titleColor?: string, titleBgColor?: string) => void;
    hideTextInputDialog: () => void;
    updateTextInputValue: (value: string) => void;
    
    // Yes/No dialog
    yesNoDialog: DialogState;
    showYesNoDialog: (title: string, message: string, onConfirm: () => void, onCancel?: () => void, color?: string, titleColor?: string, titleBgColor?: string) => void;
    hideYesNoDialog: () => void;
    
    // OK dialog
    okDialog: DialogState;
    showOkDialog: (title: string, message: string, onClose?: () => void, color?: string, titleColor?: string, titleBgColor?: string) => void;
    hideOkDialog: () => void;
    
    // Feature unavailable dialog (convenience method)
    showFeatureUnavailableDialog: (featureName: string, additionalMessage?: string, onClose?: () => void) => void;
    
    // Convenience methods for common dialog patterns
    showInfoDialog: (title: string, message: string, onClose?: () => void) => void;
    showWarningDialog: (title: string, message: string, onClose?: () => void) => void;
    showErrorDialog: (title: string, message: string, onClose?: () => void) => void;
    showSuccessDialog: (title: string, message: string, onClose?: () => void) => void;
    showDangerConfirmDialog: (title: string, message: string, onConfirm: () => void, onCancel?: () => void) => void;
    showDatabaseErrorDialog: (operation: string, onClose?: () => void) => void;
}

export function useDialogs(): UseDialogsReturn {
    const [textInputDialog, setTextInputDialog] = useState<DialogState>({
        isOpen: false,
        title: '',
        message: '',
        defaultValue: '',
    });

    const [yesNoDialog, setYesNoDialog] = useState<DialogState>({
        isOpen: false,
        title: '',
        message: '',
    });

    const [okDialog, setOkDialog] = useState<DialogState>({
        isOpen: false,
        title: '',
        message: '',
    });

    const showTextInputDialog = useCallback((
        title: string, 
        message: string, 
        defaultValue: string, 
        onConfirm: (value: string) => void,
        color?: string,
        titleColor?: string,
        titleBgColor?: string
    ) => {
        console.log("showTextInputDialog called with:", { title, message, defaultValue, color, titleColor, titleBgColor });
        setTextInputDialog({
            isOpen: true,
            title,
            message,
            defaultValue,
            color,
            titleColor,
            titleBgColor,
            onConfirm: (value) => {
                console.log("Text input dialog onConfirm called with:", value);
                if (value !== undefined) {
                    onConfirm(value);
                }
                setTextInputDialog(prev => ({ ...prev, isOpen: false }));
            },
            onCancel: () => {
                console.log("Text input dialog onCancel called");
                setTextInputDialog(prev => ({ ...prev, isOpen: false }));
            },
        });
    }, []);

    const hideTextInputDialog = useCallback(() => {
        setTextInputDialog(prev => ({ ...prev, isOpen: false }));
    }, []);

    const updateTextInputValue = useCallback((value: string) => {
        setTextInputDialog(prev => ({ ...prev, defaultValue: value }));
    }, []);

    const showYesNoDialog = useCallback((
        title: string, 
        message: string, 
        onConfirm: () => void,
        onCancel?: () => void,
        color?: string,
        titleColor?: string,
        titleBgColor?: string
    ) => {
        setYesNoDialog({
            isOpen: true,
            title,
            message,
            color,
            titleColor,
            titleBgColor,
            onConfirm: () => {
                onConfirm();
                setYesNoDialog(prev => ({ ...prev, isOpen: false }));
            },
            onCancel: () => {
                onCancel?.();
                setYesNoDialog(prev => ({ ...prev, isOpen: false }));
            },
        });
    }, []);

    const hideYesNoDialog = useCallback(() => {
        setYesNoDialog(prev => ({ ...prev, isOpen: false }));
    }, []);

    const showOkDialog = useCallback((title: string, message: string, onClose?: () => void, color?: string, titleColor?: string, titleBgColor?: string) => {
        setOkDialog({
            isOpen: true,
            title,
            message,
            color,
            titleColor,
            titleBgColor,
            onConfirm: () => {
                onClose?.();
                setOkDialog(prev => ({ ...prev, isOpen: false }));
            },
        });
    }, []);

    const hideOkDialog = useCallback(() => {
        setOkDialog(prev => ({ ...prev, isOpen: false }));
    }, []);

    const showFeatureUnavailableDialog = useCallback((
        featureName: string, 
        additionalMessage?: string, 
        onClose?: () => void
    ) => {
        const message = additionalMessage 
            ? `${featureName} is not yet implemented. ${additionalMessage}`
            : `${featureName} is not yet implemented. This feature will be available in a future update.`;
            
        showOkDialog(
            "Feature Not Available",
            message,
            onClose,
            "warning",
            undefined,
            "warning"
        );
    }, [showOkDialog]);

    // Convenience methods for common dialog patterns
    const showInfoDialog = useCallback((title: string, message: string, onClose?: () => void) => {
        showOkDialog(title, message, onClose, "info", undefined, "info");
    }, [showOkDialog]);

    const showWarningDialog = useCallback((title: string, message: string, onClose?: () => void) => {
        showOkDialog(title, message, onClose, "warning", undefined, "warning");
    }, [showOkDialog]);

    const showErrorDialog = useCallback((title: string, message: string, onClose?: () => void) => {
        showOkDialog(title, message, onClose, "danger", undefined, "danger");
    }, [showOkDialog]);

    const showSuccessDialog = useCallback((title: string, message: string, onClose?: () => void) => {
        showOkDialog(title, message, onClose, "success", undefined, "success");
    }, [showOkDialog]);

    const showDangerConfirmDialog = useCallback((
        title: string, 
        message: string, 
        onConfirm: () => void, 
        onCancel?: () => void
    ) => {
        showYesNoDialog(title, message, onConfirm, onCancel, "danger", undefined, "danger");
    }, [showYesNoDialog]);

    const showDatabaseErrorDialog = useCallback((operation: string, onClose?: () => void) => {
        showErrorDialog(
            "Database Error",
            `Failed to ${operation}. This might be due to data integrity constraints or connection issues. Please try again or contact support if the problem persists.`,
            onClose
        );
    }, [showErrorDialog]);

    return {
        textInputDialog,
        showTextInputDialog,
        hideTextInputDialog,
        updateTextInputValue,
        yesNoDialog,
        showYesNoDialog,
        hideYesNoDialog,
        okDialog,
        showOkDialog,
        hideOkDialog,
        showFeatureUnavailableDialog,
        showInfoDialog,
        showWarningDialog,
        showErrorDialog,
        showSuccessDialog,
        showDangerConfirmDialog,
        showDatabaseErrorDialog,
    };
}
