using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPO
{
    public class SPOEntity
    {
        public string SiteUrl { get; set; } = null!;
        public string LibName { get; set; } = null!;
        public string FolderName { get; set; } = null!;
        public string FolderUrl { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string DestFileName { get; set; } = null!;
        public string DocumentPath { get; set; } = null!;
        public byte[] FileByte { get; set; } = [];
        public string UserEmail { get; set; } = null!;
        public string SPOUserid { get; set; } = null!;
        public string SPOUserPass { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string PId { get; set; } = null!;
        public string Version { get; set; } = null!;
        public FileDetail FileDetails { get; set; } = null!;
    }
    public class DocumentEntity
    {
        public string FileBase64 { get; set; }
        public string DocumentName { get; set; }
        public string DocumentType { get; set; }
        public string SiteUrl { get; set; }
        public string FolderUrl { get; set; }
        public string DocumentUrl { get; set; }
        public byte[] FileContent { get; set; }
        public string DocLibrary { get; set; }
        public string ClientSubFolder { get; set; }
        public string UploadStatus { get; set; }
        public string IsEditable { get; set; }
        public string Comments { get; set; }

    }
    public class FileDetail
    {
        public FileDetail()
        {

        }
        public FileDetail(string _FileName, byte[] _FileByte)
        {
            FileName = _FileName;
            FileByte = _FileByte;
        }
        public FileDetail(string _FileName, byte[] _FileByte, string fileType)
        {
            FileName = _FileName;
            FileByte = _FileByte;
            FileType = fileType;
        }
        public string FileName { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public byte[] FileByte { get; set; } = [];
    }
    public class SPOMessage
    {
        public string Text { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public string TempValue { get; set; } = null!;
    }
}