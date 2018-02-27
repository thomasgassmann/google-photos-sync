namespace GooglePhotosClient
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Download;
    using Google.Apis.Drive.v3;
    using Google.Apis.Services;
    using Google.Apis.Util.Store;
    using GooglePhotosClient.Assets;
    using GooglePhotosClient.Extensions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public class DownloadClient
    {
        internal static string[] Scopes = { DriveService.Scope.DrivePhotosReadonly };

        private DriveService driveService;

        private UserCredential credential;

        private readonly string clientSecretSaveLocation;

        private readonly string clientSecretPath;

        public DownloadClient(string clientSecretPath, string clientSecretSaveLocation)
        {
            this.clientSecretSaveLocation = clientSecretSaveLocation;
            this.clientSecretPath = clientSecretPath;
        }

        public ClientSecrets ClientSecret
        {
            get
            {
                using (var stream = new FileStream(this.clientSecretPath, FileMode.Open, FileAccess.Read))
                {
                    return GoogleClientSecrets.Load(stream).Secrets;
                }
            }
        }

        public UserCredential UserCredentials
        {
            get
            {
                if (this.credential == null)
                {
                    this.credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        this.ClientSecret,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(this.clientSecretSaveLocation, true)).Result;
                }

                return this.credential;
            }
        }

        public DriveService DriveService
        {
            get
            {
                if (this.driveService == null)
                {
                    this.driveService = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = this.UserCredentials,
                        ApplicationName = General.Name,
                    });
                }

                return this.driveService;
            }
        }

        public void DownloadPhotos(IEnumerable<Google.Apis.Drive.v3.Data.File> files, string backupLocation, Action<IDownloadProgress> progressHandler)
        {
            var list = files.ToList();
            Console.WriteLine($"Starting download of {list.Count} files.");
            for (var i = 0; i < list.Count; i++)
            {
                var request = this.DriveService.Files.Get(list[i].Id);
                request.MediaDownloader.ProgressChanged += progressHandler;
                using (var stream = File.Create(Path.Combine(backupLocation, list[i].GetLocalIdentifierFileName())))
                {
                    request.Download(stream);
                }

                Console.WriteLine($"Downloaded file {i} of {list.Count}.");
            }
        }

        public IEnumerable<Google.Apis.Drive.v3.Data.File> IteratePhotos()
        {
            var nextPageToken = default(string);
            do
            {
                var listRequest = this.DriveService.Files.List();
                listRequest.PageSize = 100;
                listRequest.Fields = "nextPageToken, files";
                listRequest.Spaces = "photos";
                listRequest.PageToken = nextPageToken;
                var executedRequest = listRequest.Execute();
                foreach (var file in executedRequest.Files)
                {
                    Console.WriteLine(file.Name);
                    yield return file;
                }

                nextPageToken = executedRequest.NextPageToken;
            } while (nextPageToken != null);
        }
    }
}
