using Microsoft.SharePoint.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPO
{
    public class SPOService
    {

        public static byte[] DownloadFileSPO(SPOEntity objEntity)
        {
            try
            {
                byte[] fileArray = null!;
                //string fileContent = null!;

                AuthenticationManager authenticationManager = new AuthenticationManager();
                SecureString password = new();
                foreach (char c in objEntity.SPOUserPass.ToCharArray()) password.AppendChar(c);
                var uri = new Uri(objEntity.SiteUrl);


                /*CLIENT CONTEXT*/
                using (ClientContext ctxt = authenticationManager.GetContext(new Uri(objEntity.SiteUrl), objEntity.SPOUserid, password))
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    Site site = ctxt.Site;
                    Web web = ctxt.Web;
                    ResourcePath ResfilePath = ResourcePath.FromDecodedUrl(uri.AbsolutePath + (objEntity.DocumentPath.StartsWith('/') ? objEntity.DocumentPath : "/" + objEntity.DocumentPath));
                    Microsoft.SharePoint.Client.File ServerDoc = web.GetFileByServerRelativePath(ResfilePath);
                    ctxt.Load(ServerDoc);
                    ctxt.ExecuteQuery();
                    ClientResult<Stream> data = ServerDoc.OpenBinaryStream();
                    ctxt.Load(ServerDoc);
                    ctxt.ExecuteQuery();
                    using MemoryStream mStream = new();
                    if (data != null)
                    {
                        data.Value.CopyTo(mStream);
                        fileArray = mStream.ToArray();
                        //fileContent = Convert.ToBase64String(fileArray);
                    }
                }

                return fileArray;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    var response = ex.Response;
                    var dataStream = response.GetResponseStream();
                    var reader = new StreamReader(dataStream);
                    var details = reader.ReadToEnd();
                    //return "Error : " + details;
                }
                else
                {
                    //return "Error : " + ex.Message;
                }
                return null!;
            }
            catch (Exception)
            {
                //return "Error : " + ex.Message;
                return null!;
            }
        }
        public static string UploadFileSPO(SPOEntity objEntity)
        {
            string newfileurl = "";
            AuthenticationManager authenticationManager = new AuthenticationManager();
            SecureString passWord = new();
            foreach (char c in objEntity.SPOUserPass.ToCharArray()) passWord.AppendChar(c);
            try
            {
                using (ClientContext context = authenticationManager.GetContext(new Uri(objEntity.SiteUrl), objEntity.SPOUserid, passWord))
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    Site site = context.Site;
                    Web web = context.Web;
                    context.Load(site);
                    context.ExecuteQuery();

                    //for (var j = 0; j < objEntity.FileDetails.Count; j++)
                    //{
                    List docs = web.Lists.GetByTitle(objEntity.LibName);
                    context.Load(docs);
                    context.ExecuteQuery();

                    var path = objEntity.DocumentPath;
                    string[] bits = path.Split('/');
                    FolderCollection folders = docs.RootFolder.Folders;
                    context.Load(folders);
                    context.ExecuteQuery();

                    Folder clientfolder = null!;
                    if (!string.IsNullOrEmpty(path))
                    {
                        var folder = CreateFolder(context.Web, objEntity.LibName, path);
                        clientfolder = folder;
                    }
                    else
                    {
                        var folder = docs.RootFolder;
                        clientfolder = folder;
                    }

                    Microsoft.SharePoint.Client.File spfile = web.GetFileByServerRelativeUrl(clientfolder.ServerRelativeUrl + "/" + objEntity.FileDetails.FileName);
                    ListItem item = spfile.ListItemAllFields;
                    context.Load(item);
                    context.ExecuteQuery();

                    if (context.HasPendingRequest)
                    {
                        context.ExecuteQuery();
                    }
                    context.Load(clientfolder);
                    context.ExecuteQuery();
                    FileCreationInformation newFile = new FileCreationInformation();
                    newFile.ContentStream = new MemoryStream(objEntity.FileDetails.FileByte);
                    newFile.Overwrite = true;
                    newFile.Url = clientfolder.ServerRelativeUrl + '/' + objEntity.FileDetails.FileName;
                    Microsoft.SharePoint.Client.File uploadFile = clientfolder.Files.Add(newFile);
                    context.Load(uploadFile, p => p.ServerRelativeUrl);
                    //Microsoft.SharePoint.Client.File.SaveBinaryDirect(context, clientfolder.ServerRelativeUrl + "/" + objEntity.FileDetails[j].FileName, new MemoryStream(objEntity.FileDetails[j].FileByte), true);
                    context.ExecuteQuery();
                    newfileurl = uploadFile.ServerRelativeUrl;

                    var uri = new Uri(objEntity.SiteUrl);
                    //string ServerRelativeUrl = clientfolder.ServerRelativeUrl + "/" + objEntity.FileName;
                    if (newfileurl.StartsWith(uri.AbsolutePath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        newfileurl = newfileurl.Substring(uri.AbsolutePath.Length + 0);
                    }
                    else
                    {
                        newfileurl = newfileurl.ToString();
                    }

                    //}
                }
                return newfileurl;
            }
            catch (Exception)
            {
                return newfileurl;
            }
        }
        public static SPOMessage UploadFileSPOChunks(SPOEntity objEntity)
        {
            SPOMessage msg = new SPOMessage();
            try
            {
                int fileChunkSizeInMB = 10;
                using (var ctx = GetContext(objEntity.SiteUrl, objEntity.SPOUserid, objEntity.SPOUserPass))
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    Guid uploadId = Guid.NewGuid();

                    Microsoft.SharePoint.Client.Folder lib;
                    if (!TryGetFolderByServerRelativeUrl(ctx.Web, objEntity.LibName, out lib))
                    {
                        bool IsExistLib = CreateDocumentLibrary(ctx.Web, objEntity.LibName, objEntity.LibName);
                        if (!IsExistLib)
                        {
                            msg.IsSuccess = false;
                            msg.Text = "false:Document Library does not exist";
                        }
                    }

                    List docs = ctx.Web.Lists.GetByTitle(objEntity.LibName);
                    ctx.Load(docs, l => l.RootFolder);
                    ctx.Load(docs.RootFolder, f => f.ServerRelativeUrl);
                    ctx.ExecuteQuery();

                    FolderCollection folders = docs.RootFolder.Folders;
                    ctx.Load(folders);
                    ctx.ExecuteQuery();

                    Folder clientfolder = null!;
                    if (!string.IsNullOrEmpty(objEntity.DocumentPath))
                    {
                        var folder = CreateFolder(ctx.Web, objEntity.LibName, objEntity.DocumentPath);
                        clientfolder = folder;
                    }
                    else
                    {
                        var folder = docs.RootFolder;
                        clientfolder = folder;
                    }

                    var uri = new Uri(objEntity.SiteUrl);
                    Microsoft.SharePoint.Client.File uploadFile;

                    int blockSize = fileChunkSizeInMB * 1024 * 1024;

                    ctx.Load(docs.RootFolder, f => f.ServerRelativeUrl);
                    ctx.ExecuteQuery();

                    long fileSize = objEntity.FileDetails.FileByte.Length;

                    if (fileSize <= blockSize)
                    {
                        uploadFile = ctx.Web.GetFileByServerRelativeUrl(clientfolder.ServerRelativeUrl + "/" + objEntity.FileDetails.FileName);
                        ListItem item = uploadFile.ListItemAllFields;
                        ctx.Load(item);
                        ctx.ExecuteQuery();

                        if (ctx.HasPendingRequest)
                        {
                            ctx.ExecuteQuery();
                        }

                        FileCreationInformation newFile = new FileCreationInformation();
                        newFile.ContentStream = new MemoryStream(objEntity.FileDetails.FileByte);
                        newFile.Overwrite = true;
                        newFile.Url = clientfolder.ServerRelativeUrl + '/' + objEntity.FileDetails.FileName;
                        Microsoft.SharePoint.Client.File uploadableFile = clientfolder.Files.Add(newFile);
                        ctx.Load(uploadableFile, p => p.ServerRelativeUrl);
                        ctx.ExecuteQuery();
                        string newfileurl = uploadableFile.ServerRelativeUrl;
                    }
                    else
                    {
                        ClientResult<long> bytesUploaded = null!;
                        try
                        {
                            using (MemoryStream fs = new MemoryStream(objEntity.FileDetails.FileByte))
                            {
                                using (BinaryReader br = new BinaryReader(fs))
                                {
                                    byte[] buffer = new byte[blockSize];
                                    Byte[] lastBuffer = [];
                                    long fileoffset = 0;
                                    long totalBytesRead = 0;
                                    int bytesRead;
                                    bool first = true;
                                    bool last = false;

                                    while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        totalBytesRead = totalBytesRead + bytesRead;

                                        if (totalBytesRead == fileSize)
                                        {
                                            last = true;
                                            lastBuffer = new byte[bytesRead];
                                            Array.Copy(buffer, 0, lastBuffer, 0, bytesRead);
                                        }

                                        if (first)
                                        {
                                            using (MemoryStream contentStream = new MemoryStream())
                                            {
                                                FileCreationInformation fileInfo = new FileCreationInformation();
                                                fileInfo.ContentStream = contentStream;
                                                fileInfo.Url = objEntity.FileDetails.FileName;
                                                fileInfo.Overwrite = true;
                                                uploadFile = ctx.Web.GetFileByServerRelativeUrl(clientfolder.ServerRelativeUrl + "/" + objEntity.FileDetails.FileName);

                                                using (MemoryStream s = new MemoryStream(buffer))
                                                {
                                                    bytesUploaded = uploadFile.StartUpload(uploadId, s);
                                                    ctx.ExecuteQuery();
                                                    fileoffset = bytesUploaded.Value;
                                                }

                                                first = false;
                                            }
                                        }
                                        else
                                        {
                                            uploadFile = ctx.Web.GetFileByServerRelativeUrl(clientfolder.ServerRelativeUrl + System.IO.Path.AltDirectorySeparatorChar + objEntity.FileDetails.FileName);

                                            if (last)
                                            {
                                                using (MemoryStream s = new MemoryStream(lastBuffer))
                                                {
                                                    uploadFile = uploadFile.FinishUpload(uploadId, fileoffset, s);
                                                    ctx.ExecuteQuery();
                                                }
                                            }
                                            else
                                            {
                                                using (MemoryStream s = new MemoryStream(buffer))
                                                {
                                                    bytesUploaded = uploadFile.ContinueUpload(uploadId, fileoffset, s);
                                                    ctx.ExecuteQuery();
                                                    fileoffset = bytesUploaded.Value;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {

                        }
                    }
                    Microsoft.SharePoint.Client.File FileInfo;
                    var IsUpload = TryGetFileByServerRelativeUrl(ctx.Web, clientfolder.ServerRelativeUrl + "/" + objEntity.FileDetails.FileName, out FileInfo);
                    if (IsUpload)
                    {
                        msg.IsSuccess = IsUpload;
                        string ServerRelativeUrl = clientfolder.ServerRelativeUrl + "/" + objEntity.FileDetails.FileName;
                        if (ServerRelativeUrl.ToLower().StartsWith(uri.AbsolutePath.ToLower()))
                        {
                            msg.Text = ServerRelativeUrl.Substring(uri.AbsolutePath.Length + 0);
                        }
                        else
                        {
                            msg.Text = ServerRelativeUrl.ToString();
                        }
                    }
                    return msg;
                }
            }
            catch (Exception ex)
            {
                msg.IsSuccess = false;
                msg.Text = "false:" + ex.Message;
                return msg;
            }
        }
        public static SPOMessage MoveFileSPO(SPOEntity objEntity)
        {
            SPOMessage msg = new();

            try
            {
                using (var ctx = GetContext(objEntity.SiteUrl, objEntity.SPOUserid, objEntity.SPOUserPass))
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    if (!string.IsNullOrEmpty(objEntity.DocumentPath))
                    {
                        var folder = CreateFolder(ctx.Web, objEntity.LibName, objEntity.DocumentPath);
                    }

                    var uri = new Uri(objEntity.SiteUrl);
                    var IsExist = TryGetFileByServerRelativeUrl(ctx.Web, uri.AbsolutePath + objEntity.FolderUrl, out Microsoft.SharePoint.Client.File FileInfo);
                    if (IsExist)
                    {
                        var destFileUrl = string.Format("{0}/{1}/{2}", objEntity.LibName, objEntity.DocumentPath, objEntity.DestFileName);
                        destFileUrl = destFileUrl.Replace("//", "/");
                        FileInfo.MoveTo(destFileUrl, MoveOperations.Overwrite);
                        ctx.ExecuteQuery();
                        msg.IsSuccess = IsExist;
                        msg.Text = objEntity.DestFileName;
                    }

                }
            }
            catch (Exception ex)
            {
                msg.IsSuccess = false;
                msg.Text = ex.Message;
            }
            return msg;
        }
        public static bool CheckFileExistSPO(SPOEntity objEntity)
        {
            bool IsExist = new bool();
            try
            {
                using var ctx = GetContext(objEntity.SiteUrl, objEntity.SPOUserid, objEntity.SPOUserPass);
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Microsoft.SharePoint.Client.File FileInfo;
                IsExist = TryGetFileByServerRelativeUrl(ctx.Web, objEntity.DocumentPath, out FileInfo);
            }
            catch (Exception)
            {
            }
            return IsExist;
        }
        public static ClientContext GetContext(string sharepointsite, string username, string password)
        {
            try
            {

                SecureString passWord = new SecureString();
                foreach (char c in password.ToCharArray()) passWord.AppendChar(c);

                // PNP CORE METHOD
                var auth = new PnP.Framework.AuthenticationManager(username: username, password: passWord);
                var context2 = auth.GetContext(sharepointsite);
                return context2;
            }
            catch (Exception)
            {
                return null!;
            }
        }
        public static Folder CreateFolder(Web web, string listTitle, string fullFolderUrl)
        {
            if (string.IsNullOrEmpty(fullFolderUrl))
            {
                throw new ArgumentNullException("fullFolderUrl");
            }

            var list = web.Lists.GetByTitle(listTitle);
            return CreateFolderInternal(web, list.RootFolder, fullFolderUrl);
        }
        private static Folder CreateFolderInternal(Web web, Folder parentFolder, string fullFolderUrl)
        {
            var folderUrls = fullFolderUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string folderUrl = folderUrls[0];
            var curFolder = parentFolder.Folders.Add(folderUrl);
            web.Context.Load(curFolder);
            web.Context.ExecuteQuery();

            if (folderUrls.Length > 1)
            {
                var subFolderUrl = string.Join("/", folderUrls, 1, folderUrls.Length - 1);
                return CreateFolderInternal(web, curFolder, subFolderUrl);
            }
            return curFolder;
        }
        private static bool TryGetFolderByServerRelativeUrl(Web web, string serverRelativeUrl, out Microsoft.SharePoint.Client.Folder folder)
        {
            var ctx = web.Context;
            folder = web.GetFolderByServerRelativeUrl(serverRelativeUrl);
            ctx.Load(folder, f => f.Exists);
            try
            {
                ctx.ExecuteQuery();
                if (folder.Exists)
                {
                    return true;
                }
                return false;
            }
            catch (ServerException ex)
            {
                if (ex.ServerErrorTypeName == "System.IO.FileNotFoundException")
                {
                    folder = null!;
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }
        private static bool TryGetFileByServerRelativeUrl(Web web, string serverRelativeUrl, out Microsoft.SharePoint.Client.File file)
        {
            var ctx = web.Context;
            file = web.GetFileByServerRelativeUrl(serverRelativeUrl);
            ctx.Load(file, f => f.Exists);
            try
            {
                ctx.ExecuteQuery();

                if (file.Exists)
                {
                    return true;
                }
                return false;
            }
            catch (ServerException ex)
            {
                if (ex.ServerErrorTypeName == "System.IO.FileNotFoundException")
                {
                    file = null!;
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }
        public static bool CreateDocumentLibrary(Web web, string listTitle, string Description)
        {
            bool resp = false;
            ListCreationInformation creationInfo = new ListCreationInformation();
            creationInfo.Title = listTitle;
            creationInfo.Description = Description;
            creationInfo.TemplateType = (int)ListTemplateType.DocumentLibrary;

            try
            {
                List list = web.Lists.Add(creationInfo);
                list.Update();
                web.Context.ExecuteQuery();
                resp = true;
            }
            catch (Exception)
            {
                return false;
            }
            return resp;
        }

    }
    public class AuthenticationManager : IDisposable
    {
        private static readonly HttpClient httpClient = new();
        private const string tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/token";
        private const string defaultAADAppId = "c69be12b-da55-4ac6-8bf4-23f726caea89";

        // Token cache handling
        private static readonly SemaphoreSlim semaphoreSlimTokens = new(1);
        private AutoResetEvent tokenResetEvent = null!;
        private readonly ConcurrentDictionary<string, string> tokenCache = new();
        private bool disposedValue;

        internal class TokenWaitInfo
        {
            public RegisteredWaitHandle Handle = null!;
        }

        public ClientContext GetContext(Uri web, string userPrincipalName, SecureString userPassword)
        {
            var context = new ClientContext(web);

            context.ExecutingWebRequest += (sender, e) =>
            {
                string accessToken = EnsureAccessTokenAsync(new Uri($"{web.Scheme}://{web.DnsSafeHost}"), userPrincipalName, new System.Net.NetworkCredential(string.Empty, userPassword).Password).GetAwaiter().GetResult();
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            };

            return context;
        }
        public async Task<string> EnsureAccessTokenAsync(Uri resourceUri, string userPrincipalName, string userPassword)
        {
            string accessTokenFromCache = TokenFromCache(resourceUri, tokenCache);
            if (accessTokenFromCache == null)
            {
                await semaphoreSlimTokens.WaitAsync().ConfigureAwait(false);
                try
                {
                    // No async methods are allowed in a lock section
                    string accessToken = await AcquireTokenAsync(resourceUri, userPrincipalName, userPassword).ConfigureAwait(false);
                    Console.WriteLine($"Successfully requested new access token resource {resourceUri.DnsSafeHost} for user {userPrincipalName}");
                    AddTokenToCache(resourceUri, tokenCache, accessToken);

                    // Register a thread to invalidate the access token once's it's expired
                    tokenResetEvent = new AutoResetEvent(false);
                    TokenWaitInfo wi = new TokenWaitInfo();
                    wi.Handle = ThreadPool.RegisterWaitForSingleObject(
                        tokenResetEvent,
                        async (state, timedOut) =>
                        {
                            if (!timedOut)
                            {
                                TokenWaitInfo internalWaitToken = (TokenWaitInfo)state!;
                                internalWaitToken.Handle?.Unregister(null);
                            }
                            else
                            {
                                try
                                {
                                    // Take a lock to ensure no other threads are updating the SharePoint Access token at this time
                                    await semaphoreSlimTokens.WaitAsync().ConfigureAwait(false);
                                    RemoveTokenFromCache(resourceUri, tokenCache);
                                    Console.WriteLine($"Cached token for resource {resourceUri.DnsSafeHost} and user {userPrincipalName} expired");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Something went wrong during cache token invalidation: {ex.Message}");
                                    RemoveTokenFromCache(resourceUri, tokenCache);
                                }
                                finally
                                {
                                    semaphoreSlimTokens.Release();
                                }
                            }
                        },
                        wi,
                        (uint)CalculateThreadSleep(accessToken).TotalMilliseconds,
                        true
                    );

                    return accessToken;

                }
                finally
                {
                    semaphoreSlimTokens.Release();
                }
            }
            else
            {
                Console.WriteLine($"Returning token from cache for resource {resourceUri.DnsSafeHost} and user {userPrincipalName}");
                return accessTokenFromCache;
            }
        }
        private static async Task<string> AcquireTokenAsync(Uri resourceUri, string username, string password)
        {
            string resource = $"{resourceUri.Scheme}://{resourceUri.DnsSafeHost}";

            var clientId = defaultAADAppId;
            var body = $"resource={resource}&client_id={clientId}&grant_type=password&username={username}&password={password}";
            using var stringContent = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
            var result = await httpClient.PostAsync(tokenEndpoint, stringContent).ContinueWith((response) =>
            {
                return response.Result.Content.ReadAsStringAsync().Result;
            }).ConfigureAwait(false);

            var tokenResult = JsonSerializer.Deserialize<JsonElement>(result);
            var token = tokenResult.GetProperty("access_token").GetString();
            return token ?? null!;
        }
        private static string TokenFromCache(Uri web, ConcurrentDictionary<string, string> tokenCache)
        {
            if (tokenCache.TryGetValue(web.DnsSafeHost, out string? accessToken))
            {
                return accessToken;
            }
            return null!;
        }
        private static void AddTokenToCache(Uri web, ConcurrentDictionary<string, string> tokenCache, string newAccessToken)
        {
            if (tokenCache.TryGetValue(web.DnsSafeHost, out string? currentAccessToken))
            {
                tokenCache.TryUpdate(web.DnsSafeHost, newAccessToken, currentAccessToken);
            }
            else
            {
                tokenCache.TryAdd(web.DnsSafeHost, newAccessToken);
            }
        }
        private static void RemoveTokenFromCache(Uri web, ConcurrentDictionary<string, string> tokenCache)
        {
            _ = tokenCache.TryRemove(web.DnsSafeHost, out _);
        }
        private static TimeSpan CalculateThreadSleep(string accessToken)
        {
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(accessToken);
            var lease = GetAccessTokenLease(token.ValidTo);
            lease = TimeSpan.FromSeconds(lease.TotalSeconds - TimeSpan.FromMinutes(5).TotalSeconds > 0 ? lease.TotalSeconds - TimeSpan.FromMinutes(5).TotalSeconds : lease.TotalSeconds);
            return lease;
        }
        private static TimeSpan GetAccessTokenLease(DateTime expiresOn)
        {
            DateTime now = DateTime.UtcNow;
            DateTime expires = expiresOn.Kind == DateTimeKind.Utc ? expiresOn : TimeZoneInfo.ConvertTimeToUtc(expiresOn);
            TimeSpan lease = expires - now;
            return lease;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (tokenResetEvent != null)
                    {
                        tokenResetEvent.Set();
                        tokenResetEvent.Dispose();
                    }
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
