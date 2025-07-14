using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Drive.v3.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FunctionApp2
{
    public class GoogleDrivePhotoService
    {
        private readonly DriveService _driveService;

        public GoogleDrivePhotoService(string credentialsJson)
        {
            var credential = GoogleCredential.FromJson(credentialsJson)
                .CreateScoped(DriveService.Scope.DriveReadonly);

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "FunctionApp2"
            });
        }

        public async Task<List<PhotoInfo>> GetPhotosAsync(int maxResults = 10)
        {
            // First, let's check if we can access ANY files at all
            var allFilesRequest = _driveService.Files.List();
            allFilesRequest.PageSize = 5;
            allFilesRequest.Fields = "files(id,name,mimeType,trashed)";

            var allFilesResponse = await allFilesRequest.ExecuteAsync();

            // Create diagnostic info
            var diagnosticInfo = new
            {
                TotalFilesFound = allFilesResponse.Files?.Count ?? 0,
                Files = allFilesResponse.Files == null ?
                new List<object>() :
                allFilesResponse.Files.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.MimeType,
                    f.Trashed
                }).Cast<object>().ToList()
            };

            // Log diagnostic info (you can set a breakpoint here to inspect)
            Console.WriteLine($"Diagnostic Info: {JsonSerializer.Serialize(diagnosticInfo)}");

            // Now try the original query
            var request = _driveService.Files.List();
            request.Q = "mimeType contains 'image/' and trashed=false";
            request.Fields = "files(id,name,webViewLink,webContentLink,thumbnailLink,mimeType,trashed)";
            request.PageSize = maxResults;
            request.OrderBy = "createdTime desc";

            var response = await request.ExecuteAsync();

            // More diagnostic info
            var imageQueryInfo = new
            {
                ImageFilesFound = response.Files?.Count ?? 0,
                ImageFiles = response.Files == null ?
                new List<object>() :
                response.Files.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.MimeType,
                    f.Trashed
                }).Cast<object>().ToList()
            };

            Console.WriteLine($"Image Query Info: {JsonSerializer.Serialize(imageQueryInfo)}");

            // Try different queries to troubleshoot
            var simpleQuery = _driveService.Files.List();
            simpleQuery.Q = "mimeType contains 'image/'";
            simpleQuery.Fields = "files(id,name,mimeType)";
            simpleQuery.PageSize = 5;

            var simpleResponse = await simpleQuery.ExecuteAsync();

            Console.WriteLine($"Simple image query found: {simpleResponse.Files?.Count ?? 0} files");

            return response.Files?.Select(file => new PhotoInfo
            {
                Id = file.Id ?? string.Empty,
                Name = file.Name ?? string.Empty,
                WebViewLink = file.WebViewLink ?? string.Empty,
                WebContentLink = file.WebContentLink ?? string.Empty,
                ThumbnailLink = file.ThumbnailLink ?? string.Empty
            }).ToList() ?? new List<PhotoInfo>();
        }

        public async Task<string> GetPhotoBase64Async(string fileId)
        {
            var request = _driveService.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);

            var bytes = stream.ToArray();
            return Convert.ToBase64String(bytes);
        }
    }

    public class PhotoInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string WebViewLink { get; set; } = string.Empty;
        public string WebContentLink { get; set; } = string.Empty;
        public string ThumbnailLink { get; set; } = string.Empty;
    }
}