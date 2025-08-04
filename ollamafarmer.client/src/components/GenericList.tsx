import React from 'react';
import { Button, Alert } from 'reactstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faPlus, faSync } from '@fortawesome/free-solid-svg-icons';
import type { BaseListProps } from '../utils/common';
import { useDialogContext } from '../hooks/useDialogContext';

interface GenericListProps<T> extends BaseListProps<T> {
    title: string;
    emptyMessage?: string;
    showCreateButton?: boolean;
    createButtonText?: string;
    onCreateNew?: () => void;
    onDelete?: (item: T) => void;
    onEdit?: (item: T) => void;
    renderItem: (item: T, index: number) => React.ReactNode;
    renderActions?: (item: T, index: number) => React.ReactNode;
    confirmDelete?: boolean; // If true, shows confirmation dialog before deleting
    deleteMessage?: (item: T) => string; // Custom delete confirmation message
    className?: string;
}

export function GenericList<T extends { id?: string }>({
    title,
    items = [],
    loading = false,
    error = null,
    emptyMessage = "No items found",
    showCreateButton = false,
    createButtonText = "Create New",
    onCreateNew,
    onRefresh,
    onDelete,
    onEdit,
    renderItem,
    renderActions,
    confirmDelete = true,
    deleteMessage,
    className = ""
}: GenericListProps<T>) {
    const dialogs = useDialogContext();
    
    const handleDelete = (item: T) => {
        if (!onDelete) return;
        
        if (confirmDelete) {
            const message = deleteMessage 
                ? deleteMessage(item) 
                : `Are you sure you want to delete this item?`;
            
            dialogs.showYesNoDialog(
                "Confirm Delete",
                message,
                () => onDelete(item)
            );
        } else {
            onDelete(item);
        }
    };
    
    const handleEdit = (item: T) => {
        if (onEdit) {
            onEdit(item);
        }
    };
    
    if (error) {
        return (
            <div className={`container-fluid pt-4 ${className}`}>
                <div className="d-flex justify-content-between align-items-center mb-4">
                    <h1 className="display-5">{title}</h1>
                    {onRefresh && (
                        <Button color="secondary" onClick={onRefresh} disabled={loading}>
                            <FontAwesomeIcon icon={faSync} className="me-2" />
                            Retry
                        </Button>
                    )}
                </div>
                <Alert color="danger">
                    {error}
                </Alert>
            </div>
        );
    }

    return (
        <div className={`container-fluid pt-4 ${className}`}>
            <div className="d-flex justify-content-between align-items-center mb-4">
                <h1 className="display-5">{title}</h1>
                <div className="d-flex gap-2">
                    {onRefresh && (
                        <Button 
                            color="secondary" 
                            onClick={onRefresh} 
                            disabled={loading}
                            size="sm"
                        >
                            <FontAwesomeIcon icon={faSync} className="me-2" />
                            Refresh
                        </Button>
                    )}
                    {showCreateButton && onCreateNew && (
                        <Button 
                            color="primary" 
                            onClick={onCreateNew}
                            disabled={loading}
                            size="sm"
                        >
                            <FontAwesomeIcon icon={faPlus} className="me-2" />
                            {createButtonText}
                        </Button>
                    )}
                </div>
            </div>

            {loading ? (
                <div className="text-center p-4">
                    <div className="spinner-border" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                    <p className="mt-2">Loading {title.toLowerCase()}...</p>
                </div>
            ) : items.length === 0 ? (
                <Alert color="info">
                    {emptyMessage}
                </Alert>
            ) : (
                <div className="list-group">
                    {items.map((item, index) => (
                        <div key={item.id} className="list-group-item d-flex justify-content-between align-items-center">
                            <div className="flex-grow-1">
                                {renderItem(item, index)}
                            </div>
                            <div className="d-flex gap-2">
                                {renderActions ? renderActions(item, index) : (
                                    <>
                                        {onEdit && (
                                            <Button 
                                                color="secondary" 
                                                size="sm"
                                                onClick={() => handleEdit(item)}
                                                disabled={loading}
                                            >
                                                Edit
                                            </Button>
                                        )}
                                        {onDelete && (
                                            <Button 
                                                color="danger" 
                                                size="sm"
                                                onClick={() => handleDelete(item)}
                                                disabled={loading}
                                            >
                                                Delete
                                            </Button>
                                        )}
                                    </>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default GenericList;
