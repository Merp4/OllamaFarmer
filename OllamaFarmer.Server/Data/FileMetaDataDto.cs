using System.Runtime.Serialization;

namespace OllamaFarmer.Server.Data
{


    /*
     
     
export enum FileType {
    File = "file",
    Directory = "directory",
    Image = "image",
    Video = "video",
    Audio = "audio",
    Text = "text",
    Archive = "archive",
}
     
     */



    public enum FileType
    {
        [EnumMember(Value = "file")]
        File = 0,
        [EnumMember(Value = "directory")]
        Directory = 1,
        [EnumMember(Value = "image")]
        Image = 2,
        [EnumMember(Value = "video")]
        Video = 3,
        [EnumMember(Value = "audio")]
        Audio = 4,
        [EnumMember(Value = "text")]
        Text = 5,
        [EnumMember(Value = "archive")]
        Archive = 6
    }
    public class FileMetaDataDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public FileType Type { get; set; }
        public string Path { get; set; }
        public string UpdatedAt { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
    }
}
