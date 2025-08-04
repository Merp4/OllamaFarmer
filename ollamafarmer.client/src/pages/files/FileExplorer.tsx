import { useEffect, useState, useCallback } from "react";
import { toast } from "react-toastify";
import { $fetchClient } from "../../api/api";
import { getFileUrl, uploadFile, type FileMetadata } from "../../api/fileApi";
import { mapFileType } from "../../utils/fileUtils";
import { FilePreviewModal } from "./FilePreview";
import FileManager from "./FileManager";


export interface FileExplorerProps {
    initialDirectory?: string;
    onFileClick?: (file: FileMetadata) => void;
    onDirectoryClick?: (directory: FileMetadata) => void;
    onFilter?: (filter: FileMetadata) => boolean;
}

function FileExplorer({ initialDirectory, onFileClick, onDirectoryClick, onFilter }: FileExplorerProps) {
    const [currentDirectory, setCurrentDirectory] = useState<string>(initialDirectory || "/");
    
    // File preview modal state
    const [previewFile, setPreviewFile] = useState<FileMetadata | undefined>(undefined);
    const [isPreviewOpen, setIsPreviewOpen] = useState(false);
    
    // Initialize with a basic directory structure to avoid "No directory selected"
    const [files, setFiles] = useState<FileMetadata | undefined>(() => ({
        id: "0",
        name: initialDirectory || "/",
        isDirectory: true,
        path: initialDirectory || "/",
        updatedAt: new Date().toISOString(),
        hasParent: (initialDirectory || "/") !== "/",
        size: 0,
        children: []
    }));

    const fetchFiles = useCallback(async (newPath: string) => {
        const newFileMetadata: FileMetadata = {
            id: "0",
            name: newPath,
            isDirectory: true,
            path: newPath,
            updatedAt: new Date().toISOString(),
            hasParent: newPath !== "/",
            size: 0,
            children: []
        };
        console.log("Fetching files for path:", newPath);

        const response = await $fetchClient.GET("/api/File/list-meta/{path}", {
            params: { path: { path: newPath }, query: { includeSubDirectories: true } }
        });
        if (response.data) {
            newFileMetadata.children = response.data.map((item) => {
                const mappedType = mapFileType(item.type, item.mimeType);
                const fileMetadata = {
                    id: item.id ?? "",
                    name: item.name ?? "",
                    isDirectory: item.isDirectory ?? false,
                    path: item.path ?? "",
                    updatedAt: item.updatedAt ?? new Date().toISOString(),
                    size: item.size ?? 0,
                    children: [],
                    mimeType: item.mimeType ?? "",
                    type: mappedType
                };
                
                // Debug logging to help troubleshoot file type detection
                if (!item.isDirectory) {
                    console.log(`File: ${item.name}, API Type: ${item.type}, MIME: ${item.mimeType}, Mapped Type: ${mappedType}`);
                    console.log(`Extension detected: ${item.name?.toLowerCase().split('.').pop()}`);
                }
                
                return fileMetadata;
            });
            setFiles(newFileMetadata);
            console.log("Fetched files:", response.data);
        } else {
            console.warn("No files data received from API.");
        }
    }, []);

    useEffect(() => {
        fetchFiles(currentDirectory);
    }, [currentDirectory, fetchFiles]);


    const handleDirectoryClick = useCallback((directory: FileMetadata): void => {
        if(onDirectoryClick) {
            onDirectoryClick(directory);
        }
        else {
            setCurrentDirectory(directory.path);
        }
    }, [onDirectoryClick, setCurrentDirectory]);

    const togglePreview = () => setIsPreviewOpen((prev) => !prev);
    
    const handleFilePreview = useCallback((file: FileMetadata) => {
        setPreviewFile(file);
        setIsPreviewOpen(true);
        console.log("File preview requested:", file);
    }, []);



    return (
        <div className="container-fluid pt-4">
            <div className="container-fluid">
                <h1 className="display-5">File Manager</h1>
            </div>
            <FileManager
                directory={files}
                onFilter={onFilter}
                onFileClick={onFileClick}
                onDirectoryClick={handleDirectoryClick}
                onFileUpload={(file, data) => {
                    console.log("File upload requested:", file);
                    console.log("File data:", data);
                    uploadFile(data, currentDirectory)
                        .then((resp) => {
                            console.log("File upload response:", resp);
                            if (!resp.response || !resp.response.ok) {
                                throw new Error("File upload failed with status: " + resp.response?.status);
                            }
                            toast.success("File uploaded successfully!");
                            fetchFiles(currentDirectory); // Refresh the file list after upload
                        })
                        .catch((error) => {
                            console.error("File upload failed:", error);
                            toast.error("File upload failed.");
                        }).then(() => {
                            console.log("File upload completed.");
                        });
                }}
                onFileDelete={(file) => {
                    console.log("File delete requested:", file);
                    // Implement file deletion logic here
                    $fetchClient.DELETE("/api/File/delete/{fileName}", {
                        params: { path: { fileName: file.path } }
                    })
                        .then(() => {
                            toast.success("File deleted successfully!");
                            fetchFiles(currentDirectory); // Refresh the file list after deletion
                        })
                        .catch((error) => {
                            console.error("File deletion failed:", error);
                            toast.error("File deletion failed.");
                        });
                }}
                onDirectoryCreate={(dir) => {
                    console.log("Create directory requested:", dir);
                    const newDirectory: FileMetadata = {
                        id: "0",
                        name: dir.name,
                        isDirectory: true,
                        path: `${currentDirectory}/${dir.name}`.replace(/^\/+/, ''), // Ensure no leading slash
                        updatedAt: new Date().toISOString(),
                        hasParent: currentDirectory !== "/",
                        size: 0,
                        children: []
                    };
                    $fetchClient.POST("/api/File/create-directory", {
                        body: newDirectory.path
                    })
                        .then(() => {
                            toast.success("Directory created successfully!");
                            fetchFiles(currentDirectory); // Refresh the file list after creation
                        })
                        .catch((error) => {
                            console.error("Directory creation failed:", error);
                            toast.error("Directory creation failed.");
                        });
                }}
                onFileRename={(file, newName) => {
                    console.log("File rename requested:", file, "to new name:", newName);
                    const newPath = `${file.path.substring(0, file.path.lastIndexOf('/'))}/${newName}`;
                    const cleanedNewName = newPath.replace(/^\//, ''); // Remove leading slash if present
                    console.log("New file path:", newPath);
                    $fetchClient.PUT("/api/File/move/{fileName}", {
                        params: { path: { fileName: file.path }, query: { newFileName: cleanedNewName } },
                        
                    })   
                        .then(() => {
                            toast.success("File renamed successfully!");
                            fetchFiles(currentDirectory); // Refresh the file list after renaming
                        })
                        .catch((error) => {
                            console.error("File rename failed:", error);
                            toast.error("File rename failed.");
                        });
                }}
                onFilePreview={handleFilePreview}
                onFileDownload={(file) => {
                    console.log("File download requested:", file);
                    console.log("File url:", getFileUrl(file.path));
                    // Fallback download logic if no handler provided
                    const link = document.createElement('a');
                    link.href = getFileUrl(file.path);
                    //link.target = "_blank"; // Open in new tab - this ensures the download works in all browsers
                    link.download = file.name;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }}
                // onFilePreview={(file) => {
                //     console.log("File preview requested:", file);
                //     // The FileManager will handle the preview modal internally
                //     // This is just for logging/tracking purposes
                // }}
                // onFileMove={(file, targetDirectory) => {
                //     console.log("File move requested:", file, "to directory:", targetDirectory);
                //     const newPath = `${targetDirectory.path}/${file.name}`;
                //     fetchClient.POST("/api/File/move", {
                //         body: {
                //             sourcePath: file.path,
                //             targetPath: newPath
                //         }
                //     })
                //         .then(() => {
                //             toast.success("File moved successfully!");
                //             fetchFiles(currentDirectory); // Refresh the file list after moving
                //         })
                //         .catch((error) => {
                //             console.error("File move failed:", error);
                //             toast.error("File move failed.");
                //         });
                // }}
            />
            
            {/* File Preview Modal */}
            <FilePreviewModal 
                file={previewFile}
                isOpen={isPreviewOpen}
                toggle={togglePreview}
            />
        </div>
    );
}

export default FileExplorer;
