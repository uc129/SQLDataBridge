using Domain.Models;
using Domain.Shared;
using Infrastructure.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SPO;


namespace Infrastructure.Repository
{
    public class SPStorageUtilityRepository(IConfiguration configuration, IMaster_FileDetail Master_FileDetail) : ISPStorageUtility
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IMaster_FileDetail _FileDetail = Master_FileDetail;
        
        // LibName=""
        // DocumentPath=abc/2005/
        public async Task<UploadFileMessage> UploadFile(IFormFile file,  string DocumentPath, string username, string spolibname)
        {
            //var LibName = _configuration["SPO:DocLib"]!;
            var LibName = _configuration[$"SPO:{spolibname}"]!;

            UploadFileMessage msg = new();
            try
            {
                if (file.Length > 0)
                {
                    Guid guid = Guid.NewGuid();
                    string ori_filename = file.FileName;
                    string filename = guid.ToString() + Path.GetExtension(file.FileName).ToLower();
                    var spo = GetSPDetails(file, LibName, DocumentPath, filename);
                    SPOService spoh = new();
                    string AbsoluteUri = SPOService.UploadFileSPO(spo);
                    if (!string.IsNullOrEmpty(AbsoluteUri))
                    {
                        FilesEntity oFile = new()
                        {
                            FileName = ori_filename,
                            FileDesc = filename,
                            FileSize = file.Length,
                            FilePath = AbsoluteUri,
                            FileExt = Path.GetExtension(file.FileName).ToLower(),
                            ContainerName = LibName,
                            Actionby = username
                        };
                        var r = await _FileDetail.InsertFileDetails(oFile);
                        if (r.Success)
                        {
                            msg.IsSuccess = r.Success;
                            msg.Text = r.Text.Trim();
                            msg.TempValue = AbsoluteUri;
                            msg.id_file = Convert.ToInt32(msg.Text);
                        }
                    }
                }
                else
                {
                    msg.IsSuccess = false;
                    msg.Text = "File Not Found!";
                }

            }
            catch (Exception ex)
            {
                msg.IsSuccess = false;
                msg.Text = ex.Message;
            }
            return msg;
        }
        public async Task<FileContentEntity> DownloadFile(int fid)
        {
            var contentDto = new FileContentEntity();
            try
            {
                FilesEntity oFile = await _FileDetail.GetFileDetails(fid);
                SPOService spoh = new SPOService();
                var spo = GetSPDetails();
                spo.DocumentPath = oFile.FilePath;
                var byteArray = SPOService.DownloadFileSPO(spo);
                if (byteArray != null)
                {
                    //string mimeType = GetMimeTypeForFileExtension(oFile.FileName);
                    //ret = new FileContentEntity()
                    //{
                    //    Content = new MemoryStream(byteArray),
                    //    ContentType = mimeType,
                    //    Name = oFile.FileName
                    //};
                    contentDto.FileByte = byteArray;
                    contentDto.Name = oFile.FileName;
                    contentDto.NameWithExt = Path.GetFileName(oFile.FileDesc);

                    return contentDto;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return contentDto;
        }
        public SPOEntity GetSPDetails()
        {
            SPOEntity spo = new SPOEntity();
            spo.SiteUrl = _configuration["SPO:SiteUrl"]!.ToString();
            spo.SPOUserid = _configuration["SPO:Userid"]!.ToString();
            spo.SPOUserPass = _configuration["SPO:Password"]!.ToString();
            return spo;
        }
        public SPOEntity GetSPDetails(IFormFile file, string LibName, string DocumentPath, string filename)
        {
            SPOEntity spo = new()
            {
                SiteUrl = _configuration["SPO:SiteUrl"]!.ToString(),
                SPOUserid = _configuration["SPO:Userid"]!.ToString(),
                SPOUserPass = _configuration["SPO:Password"]!.ToString(),
                LibName = LibName,
                DocumentPath = DocumentPath
            };

            FileDetail fl = new()
            {
                FileName = filename
            };

            if (file.Length > 0)
            {
                using var ms = new MemoryStream();
                file.CopyTo(ms);
                fl.FileByte = ms.ToArray();
            }
            spo.FileDetails = fl;

            return spo;
        }
        public async Task<Message> DeleteFile(string fileUrl, string spolibname)
        {
            var msg = new Message { Success = false };
            var libName = _configuration[$"SPO:{spolibname}"]!;

            try
            {
                // 1. Prepare SPO Details
                var spo = GetSPDetails();
                spo.LibName = libName;
                spo.DocumentPath = fileUrl; // The Absolute URI or Relative Path depending on your SPOService logic

                // 2. Delete from Physical SharePoint Storage
                // Assuming SPOService has a Delete method similar to Upload/Download
                //bool isDeletedFromSPO = SPOService.DeleteFileSPO(spo);

                //if (isDeletedFromSPO)
                //{
                //    // 3. Delete Metadata from your SQL Database
                //    // We use the URL to identify the record in the FileDetails table
                //    var dbResult = await _FileDetail.DeleteFileDetailsByPath(fileUrl);

                //    msg.Success = dbResult.Success;
                //    msg.Text = dbResult.Success ? "File deleted successfully from SharePoint and Database." : "File deleted from SharePoint, but metadata update failed.";
                //}
                //else
                //{
                //    msg.Text = "Failed to delete file from SharePoint.";
                //}
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Delete Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return msg;
        }
    }
}
