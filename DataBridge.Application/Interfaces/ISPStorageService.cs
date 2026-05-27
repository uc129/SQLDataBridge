namespace DataBridge.Application.Interfaces;

public interface ISPStorageService
{
    Task<string> UploadFileAsync(byte[] content, string fileName, string folderPath);
}
