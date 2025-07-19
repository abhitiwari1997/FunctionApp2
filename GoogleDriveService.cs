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

        public async Task<List<PhotoInfo>> GetPhotosAsync(int maxResults = 1000)
        {
            var allPhotos = new List<PhotoInfo>();
            string? nextPageToken = null;
            var requestCount = 0;

            Console.WriteLine($"?? Starting to fetch up to {maxResults} photos...");

            do
            {
                try
                {
                    // Calculate page size - use 100 per request for optimal performance
                    var pageSize = Math.Min(100, maxResults - allPhotos.Count);
                    
                    var request = _driveService.Files.List();
                    request.Q = "mimeType contains 'image/' and trashed=false";
                    request.Fields = "nextPageToken,files(id,name,webViewLink,webContentLink,thumbnailLink,mimeType,createdTime,size)";
                    request.PageSize = pageSize;
                    request.OrderBy = "createdTime desc";
                    
                    if (!string.IsNullOrEmpty(nextPageToken))
                    {
                        request.PageToken = nextPageToken;
                    }

                    var response = await request.ExecuteAsync();
                    requestCount++;

                    if (response.Files != null)
                    {
                        foreach (var file in response.Files)
                        {
                            var photoInfo = new PhotoInfo
                            {
                                Id = file.Id ?? string.Empty,
                                Name = file.Name ?? string.Empty,
                                WebViewLink = file.WebViewLink ?? string.Empty,
                                WebContentLink = file.WebContentLink ?? string.Empty,
                                ThumbnailLink = file.ThumbnailLink ?? string.Empty,
                                MimeType = file.MimeType ?? string.Empty,
                                CreatedTime = file.CreatedTime,
                                Size = file.Size
                            };

                            allPhotos.Add(photoInfo);
                        }

                        Console.WriteLine($"?? Fetched {response.Files.Count} photos in request #{requestCount}. Total so far: {allPhotos.Count}");
                    }

                    nextPageToken = response.NextPageToken;

                    // Break if we have enough photos or no more pages
                    if (allPhotos.Count >= maxResults || string.IsNullOrEmpty(nextPageToken))
                    {
                        break;
                    }

                } catch (Exception ex)
                {
                    Console.WriteLine($"? Error in request #{requestCount}: {ex.Message}");
                    break;
                }

            } while (!string.IsNullOrEmpty(nextPageToken) && allPhotos.Count < maxResults);

            // Final summary
            Console.WriteLine($"? Successfully retrieved {allPhotos.Count} photos using {requestCount} API requests");
            
            if (allPhotos.Count > 0)
            {
                var mimeTypes = allPhotos.GroupBy(p => p.MimeType).ToList();
                Console.WriteLine($"?? File types found:");
                foreach (var group in mimeTypes)
                {
                    Console.WriteLine($"   {group.Key}: {group.Count()} files");
                }
            }

            return allPhotos.Take(maxResults).ToList();
        }

        // Lightweight method for quick access testing
        public async Task<BasicAccessInfo> TestAccessAsync()
        {
            try
            {
                var request = _driveService.Files.List();
                request.Q = "mimeType contains 'image/' and trashed=false";
                request.Fields = "files(id,name,mimeType)";
                request.PageSize = 5; // Just test with 5 files
                
                var response = await request.ExecuteAsync();

                return new BasicAccessInfo
                {
                    HasAccess = true,
                    SampleFilesFound = response.Files?.Count ?? 0,
                    Message = "Successfully connected to Google Drive"
                };
            }
            catch (Exception ex)
            {
                return new BasicAccessInfo
                {
                    HasAccess = false,
                    SampleFilesFound = 0,
                    Message = $"Access failed: {ex.Message}"
                };
            }
        }

        public async Task<string> GetPhotoBase64Async(string fileId)
        {
            var request = _driveService.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);

            var bytes = stream.ToArray();
            return Convert.ToBase64String(bytes);
        }

        // Get photos with custom filtering
        public async Task<List<PhotoInfo>> GetPhotosWithFilterAsync(string? mimeTypeFilter = null, int maxResults = 1000, string? folderName = null)
        {
            var allPhotos = new List<PhotoInfo>();
            string? nextPageToken = null;
            var requestCount = 0;

            // Build query based on filters
            var query = "trashed=false";
            
            if (!string.IsNullOrEmpty(mimeTypeFilter))
            {
                query += $" and mimeType contains '{mimeTypeFilter}'";
            }
            else
            {
                query += " and mimeType contains 'image/'";
            }

            if (!string.IsNullOrEmpty(folderName))
            {
                query += $" and parents in (select id from parents where name = '{folderName}')" ;
            }

            Console.WriteLine($"?? Searching with query: {query}");

            do
            {
                try
                {
                    var pageSize = Math.Min(100, maxResults - allPhotos.Count);
                    
                    var request = _driveService.Files.List();
                    request.Q = query;
                    request.Fields = "nextPageToken,files(id,name,webViewLink,webContentLink,thumbnailLink,mimeType,createdTime,size,parents)";
                    request.PageSize = pageSize;
                    request.OrderBy = "createdTime desc";
                    
                    if (!string.IsNullOrEmpty(nextPageToken))
                    {
                        request.PageToken = nextPageToken;
                    }

                    var response = await request.ExecuteAsync();
                    requestCount++;

                    if (response.Files != null)
                    {
                        foreach (var file in response.Files)
                        {
                            allPhotos.Add(new PhotoInfo
                            {
                                Id = file.Id ?? string.Empty,
                                Name = file.Name ?? string.Empty,
                                WebViewLink = file.WebViewLink ?? string.Empty,
                                WebContentLink = file.WebContentLink ?? string.Empty,
                                ThumbnailLink = file.ThumbnailLink ?? string.Empty,
                                MimeType = file.MimeType ?? string.Empty,
                                CreatedTime = file.CreatedTime,
                                Size = file.Size
                            });
                        }
                    }

                    nextPageToken = response.NextPageToken;

                } catch (Exception ex)
                {
                    Console.WriteLine($"? Filter search error: {ex.Message}");
                    break;
                }

            } while (!string.IsNullOrEmpty(nextPageToken) && allPhotos.Count < maxResults);

            Console.WriteLine($"? Filtered search found {allPhotos.Count} photos using {requestCount} requests");
            return allPhotos.Take(maxResults).ToList();
        }
    }

    public class PhotoInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string WebViewLink { get; set; } = string.Empty;
        public string WebContentLink { get; set; } = string.Empty;
        public string ThumbnailLink { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public DateTime? CreatedTime { get; set; }
        public long? Size { get; set; }
    }

    public class BasicAccessInfo
    {
        public bool HasAccess { get; set; }
        public int SampleFilesFound { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}