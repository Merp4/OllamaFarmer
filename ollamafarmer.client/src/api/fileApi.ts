
// File API functions - ollamafarmer.Server.Controllers.FileController

import { $fetchClient, ApiBaseUrl } from "./api";

export const ImageBaseUrl = () => {
    const baseUrl = ApiBaseUrl();
    const fileDownloadPath = "/api/File/download/";
    
    // Ensure we don't have double slashes except after the protocol
    if (baseUrl.endsWith("/")) {
        return baseUrl + fileDownloadPath.substring(1); // Remove leading slash from path
    }
    return baseUrl + fileDownloadPath;
};

export const getFileUrl = (filePath: string) => {
    // If filePath is already a URL, return it as is
    if (filePath.startsWith("http://") || filePath.startsWith("https://")) {
        return filePath;
    }
    return ImageBaseUrl() + encodeURIComponent(filePath);
};

export const uploadFile = async (file: File, path: string) => {
    const formData = new FormData();
    formData.append("file", file);
    //formData.append("fileName", file.name); // Optional, if your API requires it
    formData.append("path", path);


    return $fetchClient.POST("/api/File/upload", {
        fetch: (request) => {
            // Use the native fetch API to handle FormData
            // This is necessary because openapi-fetch does not handle FormData correctly
            // when using the `body` option directly.
            return fetch(request.url, {
                method: request.method,
                headers: {  },
                credentials: request.credentials,
                body: formData,
            });
        },
        body: {
        }
    });
}


// File Management types
export enum FileType {
    File = "file",
    Directory = "directory",
    Image = "image",
    Video = "video",
    Audio = "audio",
    Text = "text",
    Archive = "archive",
}

export interface FileMetadata {
    id: string;
    name: string;
    isDirectory: boolean;
    type?: FileType;
    path: string;
    updatedAt: string;
    size?: number;
    children?: FileMetadata[];
    hasParent?: boolean;
    parentId?: string;
    mimeType?: string;
}
