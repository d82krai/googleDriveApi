using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace GoogleDriveApi.Controllers
{
    public class HomeController : Controller
    {
        static string[] Scopes = { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "Drive API .NET Quickstart";
        DriveService _driveService;
        UserCredential credential;

        private DriveService service
        {
            get
            {
                if(_driveService == null)
                {
                    using (var stream =
                        new FileStream(Server.MapPath("~/credentials.json"), FileMode.Open, FileAccess.Read))
                    {
                        // The file token.json stores the user's access and refresh tokens, and is created
                        // automatically when the authorization flow completes for the first time.
                        string credPath = Server.MapPath("~/token.json");
                        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets,
                            Scopes,
                            "user",
                            CancellationToken.None,
                            new FileDataStore(credPath, true)).Result;
                        Console.WriteLine("Credential file saved to: " + credPath);
                    }

                    _driveService = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });
                }

                return _driveService;
            }
        }

        public ActionResult Index()
        {
            var files = GetFiles();
            var filesInFolder = GetFilesInFolder("0B0L8q8KUFG-lNXdEMHYyYlA3eDA");
            var allFolders = GetAllFolders();

            return View();
        }

        #region Drive API related functions

        public List<Google.Apis.Drive.v3.Data.File> GetFiles()
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 1000;
            //listRequest.Fields = "nextPageToken, files(id, name)";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                .Files;
            Console.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                return files.ToList();
            }
            else
            {
                return null;
            }
        }

        public List<Google.Apis.Drive.v3.Data.File> GetFilesInFolder(string folderId)
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 1000;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.Q = "'"+ folderId + "' in parents";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                .Files;
            Console.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                return files.ToList();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns all files inside a directory
        /// </summary>
        /// <param name="dir">Directory name</param>
        /// <returns></returns>
        public IList<Google.Apis.Drive.v3.Data.File> GetAllFolders()
        {
                FilesResource.ListRequest request = service.Files.List();
                request.PageSize = 1000;
                request.Fields = "nextPageToken, files(name, size, id, parents)";
                request.Q = "mimeType = 'application/vnd.google-apps.folder' and trashed = false";

                var result = request.Execute();

                return result.Files;
        }

        /// <summary>
        /// Returns all files inside a directory
        /// </summary>
        /// <param name="dir">Directory name</param>
        /// <returns></returns>
        public IList<Google.Apis.Drive.v3.Data.File> GetFiles(string dir)
        {
            if (DirectoryExists(dir))
            {
                FilesResource.ListRequest request = service.Files.List();
                request.Fields = "nextPageToken, files(name, size, id)";
                request.Q = "mimeType != 'application/vnd.google-apps.folder' and trashed = false";

                var result = request.Execute();

                return result.Files;
            }

            return null;
        }


        /// <summary>
        /// Creates a request to check if a file exists in Drive.
        /// </summary>
        /// <param name="fileName">The name of the file to check</param>
        /// <returns></returns>
        public bool FileExists(string fileName)
        {
            FilesResource.ListRequest request = service.Files.List();
            request.Q = $"mimeType!='application/vnd.google-apps.folder' and name='{fileName}'";

            var result = request.Execute();

            if (result.Files.Count > 0)
                // File found
                return true;
            else
                // File not found
                throw new FileNotFoundException($"{fileName} not found.");
        }


        /// <summary>
        /// Creates a request to check if a directory exists in Drive.
        /// </summary>
        /// <param name="dirName">The name of the file to check</param>
        /// <returns></returns>
        public bool DirectoryExists(string dirName)
        {
            FilesResource.ListRequest request = service.Files.List();
            request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{dirName}'";

            var result = request.Execute();

            if (result.Files.Count > 0)
                // Directory found
                return true;
            else
                // Directory not found
                return false;
        }

        /// <summary>
        /// Returns the ID of a file/directory from Drive
        /// </summary>
        /// <param name="name">The name of the directory/file to get its id</param>
        /// <param name="isDirectory">The boolean to indicate if the id to search must be of a directory</param>
        /// <returns></returns>
        public string GetID(string name, bool isDirectory = false)
        {
            FilesResource.ListRequest request = service.Files.List();

            // Search for directory if it exists
            if (isDirectory)
            {
                if (DirectoryExists(name))
                {
                    request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{name}'";
                    var result = request.Execute();

                    return result.Files[0].Id;
                }
            }
            // Search for file
            else
            {
                if (FileExists(name))
                {
                    request.Q = $"mimeType!='application/vnd.google-apps.folder' and name='{name}'";
                    var result = request.Execute();

                    return result.Files[0].Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the size of a file
        /// </summary>
        /// <param name="name">The name of the file</param>
        /// <returns></returns>
        public long GetFileSize(string name)
        {
            // List files
            FilesResource.ListRequest request = service.Files.List();
            request.Fields = "nextPageToken, files(name, size)";
            request.Q = $"'{GetID(name, true)}' in parents and trashed=false";
            var result = request.Execute();
            var files = result.Files;

            long size = 0;
            foreach (var file in files)
            {
                size += file.Size.Value;
                return size;
            }

            return 0;
        }

        //public async Task<string> AddFile(string sourceFilePath, string destinationFilePath, string uploadedBy="")
        //{
        //    if (System.IO.File.Exists(sourceFilePath))
        //    {
        //        Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File();
        //        body.Name = System.IO.Path.GetFileName(sourceFilePath);
        //        //body.Description = _descrp;
        //        body.Description = "Uploaded on " + DateTime.Now.ToString();
        //        if(!string.IsNullOrWhiteSpace(uploadedBy))
        //        {
        //            body.Description += Environment.NewLine + ", Uploaded by " + uploadedBy;
        //        }
        //        body.MimeType = GetMimeType(sourceFilePath);
        //        body.Parents = new List<string>() { _parent } ;

        //        byte[] byteArray = System.IO.File.ReadAllBytes(sourceFilePath);
        //        System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
        //        try
        //        {
        //            FilesResource.InsertMediaUpload request = _service.Files.Insert(body, stream, GetMimeType(sourceFilePath));
        //            request.Upload();
        //            return request.ResponseBody;
        //        }
        //        catch (Exception e)
        //        {
        //            MessageBox.Show(e.Message, "Error Occured");
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("The file does not exist.", "404");
        //    }
        //}

        private static string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        //public async Task<string> AddFile(string sourceFilePath, string destinationFilePath)
        public string AddFile(string sourceFilePath, string destinationFilePath, string folderId)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(sourceFilePath),
                Parents = new List<string>
                {
                    folderId
                },
                MimeType = GetMimeType(sourceFilePath)
            };

            var folderFileId = GetFolderFiles(service, folderId, Path.GetFileName(sourceFilePath)); // Проверяю, есть ли этот файл на Google Drive.

            if (folderFileId.Count > 0)
            {
                service.Files.Delete(folderFileId[0].Id).Execute(); // Просто обновить файл не получилось. Удаляю если файл с таким именем уже есть.
            }

            FilesResource.CreateMediaUpload request;

            using (var stream = new FileStream(sourceFilePath, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, fileMetadata.MimeType);
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            return file.Id;
        }

        public void AddFile(Stream sourceFileStream, string sourceFilePath, string folderId)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(sourceFilePath),
                Parents = new List<string>
                {
                    folderId
                },
                MimeType = GetMimeType(sourceFilePath)
            };

            FilesResource.CreateMediaUpload request;
            request = service.Files.Create(fileMetadata, sourceFileStream, fileMetadata.MimeType);
            request.Fields = "id";
            request.Upload();
        }

        private IList<Google.Apis.Drive.v3.Data.File> GetFolderFiles(DriveService service, string folderId, string fileName)
        {
            // Параметры для запроса по поиску файлов.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = $"'{folderId}' in parents and name = '{fileName}' and trashed = false";
            listRequest.Fields = "nextPageToken, files(id, name)";

            return listRequest.Execute().Files; // Список найденых файлов. В идеале там должен быть только один элемент.
        }

        #endregion


    }
}