import React, { useEffect, useMemo, useState } from "react";
import { 
    formatFileSize, 
    getParentPath, 
    mapFileType,
    getFileTypeDisplayName
} from "../../utils/fileUtils";
import { faFileImage, faFileVideo, faFileAudio, faFileText, faFileArchive, faFolder, faFileDownload, faFilePen, faTrash, faMagnifyingGlass } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { FileType, type FileMetadata } from "../../api/fileApi";
import { useDialogContext } from "../../hooks/useDialogContext";

// File operation types
export interface FileOperations {
    onFileClick?: (file: FileMetadata) => void;
    onDirectoryClick?: (directory: FileMetadata) => void;
    onFileUpload?: (file: FileMetadata, data: File) => void;
    onFileDelete?: (file: FileMetadata) => void;
    onFileRename?: (file: FileMetadata, newName: string) => void;
    onFilePreview?: (file: FileMetadata) => void;
    onDirectoryCreate?: (directory: FileMetadata) => void;
}

/**
 * Checks if a file can be previewed
 */
function canPreviewFile(file: FileMetadata): boolean {
    const fileType = mapFileType(file.type, file.mimeType);
    return [FileType.Image, FileType.Video, FileType.Audio, FileType.Text].includes(fileType);
}

export interface FileManagerProps extends FileOperations {
    directory?: FileMetadata;
    excludeSubDirectories?: boolean;
    onFileSearch?: (query: string) => void;
    onFileSort?: (sortBy: string, sortOrder: 'asc' | 'desc') => void;
    onFileFilter?: (filter: string) => void;
    onFilter?: (file: FileMetadata) => boolean;
    onFileDownload?: (file: FileMetadata) => void;
    onFileMove?: (file: FileMetadata, newPath: string) => void;
    onFileCopy?: (file: FileMetadata, newPath: string) => void;
    previewOnClick?: boolean; // If true, clicking a file will trigger preview instead of click handler
}


// Icon component for file types
function FileIcon({ type }: { type?: FileType; fileName: string }) {
    const fileType = type || FileType.File;

    return (
        <span style={{ marginRight: 8 }}>
            {fileType === FileType.Image ? <FontAwesomeIcon icon={faFileImage} /> : null}
            {fileType === FileType.Video ? <FontAwesomeIcon icon={faFileVideo} /> : null}
            {fileType === FileType.Audio ? <FontAwesomeIcon icon={faFileAudio} /> : null}
            {fileType === FileType.Text ? <FontAwesomeIcon icon={faFileText} /> : null}
            {fileType === FileType.Archive ? <FontAwesomeIcon icon={faFileArchive} /> : null}
            {fileType === FileType.Directory ? <FontAwesomeIcon icon={faFolder} /> : null}
        </span>
    );
}

// File row component
const FileRow = React.memo(function FileRow({
    file,
    onFileClick,
    onDirectoryClick,
    onFileDelete,
    onFileRename,
    onFileDownload,
    onFilePreview,
    onFileMove,
    onFileCopy,
    previewOnClick
}: {
    file: FileMetadata;
    onFileClick?: (file: FileMetadata) => void;
    onDirectoryClick?: (directory: FileMetadata) => void;
    onFileDelete?: (file: FileMetadata) => void;
    onFileRename?: (file: FileMetadata, newName: string) => void;
    onFileDownload?: (file: FileMetadata) => void;
    onFilePreview?: (file: FileMetadata) => void;
    onFileMove?: (file: FileMetadata, newPath: string) => void;
    onFileCopy?: (file: FileMetadata, newPath: string) => void;
    previewOnClick?: boolean; // If true, clicking a file will trigger preview instead of click handler
}) {
    const dialogs = useDialogContext();

    const handleClick = () => {
        if (file.isDirectory) {
            if (onDirectoryClick) { onDirectoryClick(file); }
        } else {
            if (onFileClick) {
                onFileClick(file);
            } else if (previewOnClick && canPreviewFile(file)) {
                // Default to preview if no file click handler is provided and file can be previewed
                onFilePreview?.(file);
            }
        }
    };

    const handleDelete = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (onFileDelete) {
            dialogs.showDangerConfirmDialog(
                "Confirm Delete",
                `Are you sure you want to delete "${file.name}"? This action cannot be undone.`,
                () => onFileDelete(file)
            );
        }
    };
    const handleRename = (e: React.MouseEvent) => {
        e.stopPropagation();
        dialogs.showTextInputDialog(
            "Rename File",
            "Enter new name:",
            file.name,
            (newName) => {
                if (newName && onFileRename) onFileRename(file, newName);
            }
        );
    };
    const handleDownload = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (onFileDownload) onFileDownload(file);
    };
    const handlePreview = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (onFilePreview) onFilePreview(file);
    };
    const handleMove = (e: React.MouseEvent) => {
        e.stopPropagation();
        dialogs.showTextInputDialog(
            "Move File",
            "Enter new path:",
            file.path,
            (newPath) => {
                if (newPath && onFileMove) onFileMove(file, newPath);
            }
        );
    };
    const handleCopy = (e: React.MouseEvent) => {
        e.stopPropagation();
        dialogs.showTextInputDialog(
            "Copy File",
            "Enter copy destination path:",
            file.path,
            (newPath) => {
                if (newPath && onFileCopy) onFileCopy(file, newPath);
            }
        );
    };

    return (
        <tr style={{ cursor: "pointer" }} onClick={handleClick}>
            <td>
                <FileIcon 
                    type={file.isDirectory ? FileType.Directory : file.type} 
                    fileName={file.name} 
                />
                {file.name}
                {/* Action buttons */}
                <span className="mx-2 float-end">
                    <span className="m-1 ">
                        {!file.isDirectory && (
                            <>
                                {onFilePreview && canPreviewFile(file) && (<button className="btn btn-sm btn-primary me-1" title="Preview" onClick={handlePreview}><FontAwesomeIcon icon={faMagnifyingGlass} /></button>)}
                                {onFileDownload && (<button className="btn btn-sm btn-success me-1" title="Download" onClick={handleDownload}><FontAwesomeIcon icon={faFileDownload} /></button>)}
                            </>
                        )}
                    </span>
                    <span className="m-1 ">
                        {onFileRename && (<button className="btn btn-sm btn-secondary me-1" title="Rename" onClick={handleRename}><FontAwesomeIcon icon={faFilePen} /></button>)}
                        {onFileDelete && (<button className="btn btn-sm btn-danger me-1" title="Delete" onClick={handleDelete}><FontAwesomeIcon icon={faTrash} /></button>)}
                        {onFileMove && (<button className="btn btn-sm btn-warning me-1" title="Move" onClick={handleMove}><FontAwesomeIcon icon={faFolder} /></button>)}
                        {onFileCopy && (<button className="btn btn-sm btn-primary" title="Copy" onClick={handleCopy}><FontAwesomeIcon icon={faFileArchive} /></button>)}
                    
                    </span>
                </span>
            </td>
            <td className="text-capitalize">
                {file.isDirectory ? 'Folder' : getFileTypeDisplayName(mapFileType(file.type, file.mimeType))}
            </td>
            <td>{file.isDirectory ? "-" : formatFileSize(file.size, 2) ?? "-"}</td>
            <td>{file.updatedAt}</td>
        </tr>
    );
});

// FileManager component
const FileManager: React.FC<FileManagerProps> = ({
    directory,
    onFileClick,
    onDirectoryClick,
    onDirectoryCreate,
    onFileUpload,
    onFileDelete,
    onFileRename,
    onFileDownload,
    onFileMove,
    onFileCopy,
    onFileSearch,
    onFileSort,
    onFileFilter,
    onFilePreview,
    onFilter,
    previewOnClick = true, // Default to true for backward compatibility
}) => {
    const dialogs = useDialogContext();
    const [currentDirectory, setCurrentDirectory] = useState<FileMetadata | undefined>(directory);

    useEffect(() => {
        setCurrentDirectory(directory);
    }, [directory]);

    // Parent directory row logic
    const parentDirectoryFile: FileMetadata = {
        ...currentDirectory,
        id: "parent",
        name: "..",
        isDirectory: true,
        type: FileType.Directory,
        updatedAt: "",
        size: 0,
        path: getParentPath(currentDirectory?.path),
        hasParent: true,
        children: [],
    };

    // Directory/file click handlers
    const handleDirectoryClick = (dir: FileMetadata) => {
        if (onDirectoryClick) {
            onDirectoryClick(dir);
        } else {
            setCurrentDirectory(dir);
        }
    };

    const handleFileClick = (file: FileMetadata) => {
        if (onFileClick) {
            onFileClick(file);
        } else if (canPreviewFile(file)) {
            // Default to preview if no file click handler is provided and file can be previewed
            handleFilePreview(file);
        }
    };

    // Menu bar handlers
    const handleCreateFolder = () => {
        if (onDirectoryCreate && currentDirectory) {
            dialogs.showTextInputDialog(
                "Create Folder",
                "Enter new folder name:",
                "",
                (folderName) => {
                    if (folderName) {
                        onDirectoryCreate({
                            id: "",
                            name: folderName,
                            isDirectory: true,
                            type: FileType.Directory,
                            path: currentDirectory.path + "/" + folderName,
                            updatedAt: new Date().toISOString(),
                            size: 0,
                            children: [],
                            hasParent: true,
                            parentId: currentDirectory.id,
                        });
                    }
                }
            );
        }
    };

    const handleUploadFile = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (onFileUpload && e.target.files && e.target.files.length > 0 && currentDirectory) {
            const file = e.target.files[0];
            onFileUpload({
                id: "",
                name: file.name,
                isDirectory: false,
                type: FileType.File,
                path: currentDirectory.path + "/" + file.name,
                updatedAt: new Date().toISOString(),
                size: file.size,
                hasParent: true,
                parentId: currentDirectory.id,
            }, file);
        }
    };

    // Sorting and filtering state
    const [sortBy, setSortBy] = useState<string>("name");
    const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');
    const [filter, setFilter] = useState<string>("");
    const [searchQuery, setSearchQuery] = useState("");
    
    const handleFilePreview = (file: FileMetadata) => {
        if (!canPreviewFile(file)) {
            return;
        }        
        
        if (onFilePreview) {
            onFilePreview(file);
        }
    };

    // Sorting handler
    const handleSort = (column: string) => {
        let order: 'asc' | 'desc' = sortOrder;
        if (sortBy === column) {
            order = sortOrder === 'asc' ? 'desc' : 'asc';
        } else {
            order = 'asc';
        }
        setSortBy(column);
        setSortOrder(order);
        if (onFileSort) onFileSort(column, order);
    };

    // Filtering handler
    const handleFilter = (e: React.ChangeEvent<HTMLInputElement>) => {
        setFilter(e.target.value);
        if (onFileFilter) onFileFilter(e.target.value);
    };

    // Search handler
    const handleSearch = (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        if (onFileSearch) onFileSearch(searchQuery);
    };

    // Memoize filtered and sorted children
    const filteredSortedChildren = useMemo(() => {
        if (!currentDirectory || !currentDirectory.children) return [];
        return currentDirectory.children
            .filter(f =>
                (!filter || f.name.toLowerCase().includes(filter.toLowerCase())) &&
                onFilter?.(f) !== false &&
                (!searchQuery || f.name.toLowerCase().includes(searchQuery.toLowerCase()))
            )
            .slice()
            .sort((a, b) => {
                if (a.isDirectory && !b.isDirectory) return -1;
                if (!a.isDirectory && b.isDirectory) return 1;
                if (sortBy === "name") return sortOrder === "asc" ? a.name.localeCompare(b.name) : b.name.localeCompare(a.name);
                if (sortBy === "type") {
                    const typeA = a.isDirectory ? 'Folder' : getFileTypeDisplayName(mapFileType(a.type, a.mimeType));
                    const typeB = b.isDirectory ? 'Folder' : getFileTypeDisplayName(mapFileType(b.type, b.mimeType));
                    return sortOrder === "asc" ? typeA.localeCompare(typeB) : typeB.localeCompare(typeA);
                }
                if (sortBy === "size") return sortOrder === "asc" ? (a.size ?? 0) - (b.size ?? 0) : (b.size ?? 0) - (a.size ?? 0);
                if (sortBy === "updatedAt") return sortOrder === "asc"
                    ? new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime()
                    : new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
                return 0;
            });
    }, [currentDirectory, filter, onFilter, searchQuery, sortBy, sortOrder]);

    if (!currentDirectory) {
        return (
            <div className="container-fluid">
                <div>No directory selected.</div>
            </div>
        );
    }

    return (
        <div className="container-fluid">
            {/* Menu Bar */}
            <div className="mb-2 d-flex align-items-center gap-2">
                {onDirectoryCreate && (<button className="btn btn-sm btn-primary" onClick={handleCreateFolder}>
                    New Folder
                </button>)}
                {onFileUpload && (<label className="btn btn-sm btn-secondary mb-0">
                    Upload File
                    <input
                        type="file"
                        style={{ display: "none" }}
                        onChange={handleUploadFile}
                    />
                </label>)}
                {onFileSearch && (<form className="ms-auto d-flex" onSubmit={handleSearch}>
                    <input
                        type="text"
                        className="form-control form-control-sm"
                        placeholder="Search files..."
                        value={searchQuery}
                        onChange={e => setSearchQuery(e.target.value)}
                        style={{ width: 200 }}
                    />
                    <button className="btn btn-sm btn-outline-secondary ms-1" type="submit">
                        Search
                    </button>
                </form>)}
                {onFileFilter && (<>
                <input
                    type="text"
                    className="form-control form-control-sm ms-2"
                    placeholder="Filter..."
                    value={filter}
                    onChange={handleFilter}
                    style={{ width: 120 }}
                /></>)}
            </div>
            
            {/* File Table */}
            <table className="table table-hover table-sm">
                <thead>
                    <tr>
                        <th style={{ cursor: "pointer" }} onClick={() => handleSort("name")}>
                            Name {sortBy === "name" ? (sortOrder === "asc" ? "▲" : "▼") : ""}
                        </th>
                        <th style={{ cursor: "pointer" }} onClick={() => handleSort("type")}>
                            Type {sortBy === "type" ? (sortOrder === "asc" ? "▲" : "▼") : ""}
                        </th>
                        <th style={{ cursor: "pointer" }} onClick={() => handleSort("size")}>
                            Size {sortBy === "size" ? (sortOrder === "asc" ? "▲" : "▼") : ""}
                        </th>
                        <th style={{ cursor: "pointer" }} onClick={() => handleSort("updatedAt")}>
                            Last Modified {sortBy === "updatedAt" ? (sortOrder === "asc" ? "▲" : "▼") : ""}
                        </th>
                    </tr>
                </thead>
                <tbody>
                    {currentDirectory.hasParent && (
                        <FileRow
                            key={-1}
                            file={parentDirectoryFile}
                            onFileClick={handleFileClick}
                            onDirectoryClick={handleDirectoryClick}
                            previewOnClick={previewOnClick}
                        />
                    )}
                    {filteredSortedChildren.length === 0 ? (
                        <tr>
                            <td colSpan={4} className="text-center text-muted">
                                No files or directories.
                            </td>
                        </tr>
                    ) : (
                        filteredSortedChildren.map((file) => (
                            <FileRow
                                key={file.id}
                                file={file}
                                onFileClick={handleFileClick}
                                onDirectoryClick={handleDirectoryClick}
                                onFileDelete={onFileDelete}
                                onFileRename={onFileRename}
                                onFileDownload={onFileDownload}
                                onFilePreview={handleFilePreview}
                                onFileMove={onFileMove}
                                onFileCopy={onFileCopy}
                                previewOnClick={previewOnClick}
                            />
                        ))
                    )}
                </tbody>
            </table>
        </div>
    );
};

export default FileManager;
