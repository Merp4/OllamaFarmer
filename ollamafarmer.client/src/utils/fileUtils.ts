import { FileType } from "../api/fileApi";


/**
 * Determines file type from MIME type
 */
export function getFileTypeFromMimeType(mimeType: string): FileType {
    if (mimeType.startsWith('image/')) return FileType.Image;
    if (mimeType.startsWith('video/')) return FileType.Video;
    if (mimeType.startsWith('audio/')) return FileType.Audio;
    if (mimeType.startsWith('text/') || 
        mimeType === 'application/json' || 
        mimeType === 'application/xml' ||
        mimeType.includes('javascript') ||
        mimeType.includes('typescript')) {
        return FileType.Text;
    }
    if (mimeType.includes('zip') || 
        mimeType.includes('rar') || 
        mimeType.includes('archive') ||
        mimeType.includes('compressed')) {
        return FileType.Archive;
    }
    return FileType.File;
}

/**
 * Maps API file type to our FileType enum with fallbacks
 */
export function mapFileType(
    apiType: string | undefined, 
    mimeType: string | undefined
): FileType {

    // Primary: Use API type if available (most reliable)
    if (apiType) {
        switch (apiType.toLowerCase()) {
            case 'file': return FileType.File;
            case 'directory': return FileType.Directory;
            case 'image': return FileType.Image;
            case 'video': return FileType.Video;
            case 'audio': return FileType.Audio;
            case 'text': return FileType.Text;
            case 'archive': return FileType.Archive;
            default: break; // Fall through to other methods
        }
    }

    if (mimeType) {
        const mimeTypeResult = getFileTypeFromMimeType(mimeType);
        if (mimeTypeResult !== FileType.File) {
            return mimeTypeResult;
        }
    }

    return FileType.File;
}


/**
 * Formats file size for display
 */
export function formatFileSize(bytes: number | undefined, decimals = 2): string {
    if (!bytes || bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

/**
 * Gets parent directory path
 */
export function getParentPath(path: string | undefined): string {
    if (!path) return "/";
    const parts = path.split('/').filter(Boolean);
    if (parts.length === 0) return "/";
    parts.pop();
    return "/" + parts.join('/');
}

/**
 * Validates file name
 */
export function isValidFileName(name: string): boolean {
    // Check for invalid characters
    const invalidChars = /[<>:"/\\|?*]/;
    if (invalidChars.test(name)) return false;
    
    // Check for reserved names (Windows)
    const reservedNames = ['CON', 'PRN', 'AUX', 'NUL', 'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9', 'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9'];
    if (reservedNames.includes(name.toUpperCase())) return false;
    
    // Check length
    if (name.length === 0 || name.length > 255) return false;
    
    return true;
}

/**
 * Gets a human-readable display name for a file type
 */
export function getFileTypeDisplayName(fileType: FileType): string {
    switch (fileType) {
        case FileType.Directory: return 'Folder';
        case FileType.Image: return 'Image';
        case FileType.Video: return 'Video';
        case FileType.Audio: return 'Audio';
        case FileType.Text: return 'Text';
        case FileType.Archive: return 'Archive';
        case FileType.File:
        default: return 'File';
    }
}
