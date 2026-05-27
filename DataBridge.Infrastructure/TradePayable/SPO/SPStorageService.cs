using DataBridge.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using SPO;

namespace DataBridge.Infrastructure.TradePayable.SPO;

public class SPStorageService(IConfiguration config) : ISPStorageService
{
    public Task<string> UploadFileAsync(byte[] content, string fileName, string folderPath)
    {
        return Task.Run(() =>
        {
            try
            {
                var spo = new SPOEntity
                {
                    SiteUrl      = config["SPO:SiteUrl"]!,
                    SPOUserid    = config["SPO:Userid"]!,
                    SPOUserPass  = config["SPO:Password"]!,
                    LibName      = config["SPO:DocLib"] ?? "S&AEvidences",
                    DocumentPath = folderPath,
                    FileDetails  = new FileDetail(fileName, content),
                };

                return SPOService.UploadFileSPO(spo) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        });
    }
}
