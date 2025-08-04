
// Common UI types
export interface BaseModalProps {
    isOpen: boolean;
    onClose: () => void;
}

export interface SelectableItem {
    id: string;
    name: string;
    selected?: boolean;
}

// Common API response patterns
export interface PaginatedResponse<T> {
    cursor: number | null;
    filteredCount: number | null;
    data?: T[];
}

// Common component props
export interface BaseListProps<T> {
    items?: T[];
    loading?: boolean;
    error?: string | null;
    onItemSelect?: (item: T) => void;
    onRefresh?: () => void;
}
