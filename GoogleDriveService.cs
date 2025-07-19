using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.PhotosLibrary.v1;
using Google.Apis.Services;
using Google.Apis.Drive.v3.Data;
using Google.Apis.PhotosLibrary.v1.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FunctionApp2
{
    public class GoogleDrivePhotoService
    {
        private readonly DriveService _driveService;
        private readonly PhotosLibraryService _photosService;

        public GoogleDrivePhotoService(string credentialsJson)
        {
            try
            {
                Console.WriteLine($"🔍 [DEBUG] Initializing GoogleDrivePhotoService...");
                Console.WriteLine($"🔍 [DEBUG] Credentials JSON length: {credentialsJson?.Length ?? 0}");
                
                var credential = GoogleCredential.FromJson(credentialsJson)
                    .CreateScoped(DriveService.Scope.DriveReadonly, PhotosLibraryService.Scope.PhotoslibraryReadonly);

                Console.WriteLine($"🔍 [DEBUG] Credential created successfully");

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "FunctionApp2"
                });

                Console.WriteLine($"🔍 [DEBUG] Drive service initialized");

                _photosService = new PhotosLibraryService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "FunctionApp2"
                });

                Console.WriteLine($"✅ [DEBUG] Photos service initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [DEBUG] Error initializing GoogleDrivePhotoService: {ex.Message}");
                Console.WriteLine($"❌ [DEBUG] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"❌ [DEBUG] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<PhotoInfo>> GetPhotosAsync(int maxResults = 1000)
        {
            var allPhotos = new List<PhotoInfo>();
            
            Console.WriteLine($"🔍 [DEBUG] Starting combined photo fetch, maxResults: {maxResults}");

            try
            {
                // Get photos from Google Drive
                Console.WriteLine($"🔍 [DEBUG] Fetching Google Drive photos...");
                var drivePhotos = await GetDrivePhotosAsync(maxResults / 2);
                allPhotos.AddRange(drivePhotos);
                Console.WriteLine($"📂 [DEBUG] Retrieved {drivePhotos.Count} photos from Google Drive");

                // Get photos from Google Photos
                var remainingLimit = maxResults - allPhotos.Count;
                Console.WriteLine($"🔍 [DEBUG] Remaining limit for Google Photos: {remainingLimit}");
                
                //if (remainingLimit > 0)
                //{
                //    Console.WriteLine($"🔍 [DEBUG] Fetching Google Photos...");
                //    var googlePhotos = await GetGooglePhotosAsync(remainingLimit);
                //    allPhotos.AddRange(googlePhotos);
                //    Console.WriteLine($"📸 [DEBUG] Retrieved {googlePhotos.Count} photos from Google Photos");
                //}

                Console.WriteLine($"🔍 [DEBUG] Total photos before sorting: {allPhotos.Count}");
                
                // Sort all photos by creation time (newest first)
                var sortedPhotos = allPhotos
                    .Where(p => p.CreatedTime.HasValue)
                    .OrderByDescending(p => p.CreatedTime)
                    .Take(maxResults)
                    .ToList();

                Console.WriteLine($"✅ [DEBUG] Total photos after sorting and filtering: {sortedPhotos.Count}");
                
                return sortedPhotos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [DEBUG] Critical error in GetPhotosAsync: {ex.Message}");
                Console.WriteLine($"❌ [DEBUG] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"❌ [DEBUG] Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let the calling method handle it
            }
        }

        private async Task<List<PhotoInfo>> GetDrivePhotosAsync(int maxResults)
        {
            var allPhotos = new List<PhotoInfo>();
            string? nextPageToken = null;
            var requestCount = 0;

            do
            {
                try
                {
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
                                Size = file.Size,
                                Source = "Google Drive"
                            };

                            allPhotos.Add(photoInfo);
                        }
                    }

                    nextPageToken = response.NextPageToken;

                    if (allPhotos.Count >= maxResults || string.IsNullOrEmpty(nextPageToken))
                    {
                        break;
                    }

                } catch (Exception ex)
                {
                    Console.WriteLine($"❌ Drive API error: {ex.Message}");
                    break;
                }

            } while (!string.IsNullOrEmpty(nextPageToken) && allPhotos.Count < maxResults);

            return allPhotos;
        }

        

        // Lightweight method for quick access testing
        public async Task<BasicAccessInfo> TestAccessAsync()
        {
            var driveAccess = await TestDriveAccessAsync();
            var photosAccess = await TestPhotosAccessAsync();

            return new BasicAccessInfo
            {
                HasAccess = driveAccess.HasAccess || photosAccess.HasAccess,
                SampleFilesFound = driveAccess.SampleFilesFound + photosAccess.SampleFilesFound,
                Message = $"Drive: {driveAccess.Message} | Photos: {photosAccess.Message}"
            };
        }

        private async Task<BasicAccessInfo> TestDriveAccessAsync()
        {
            try
            {
                var request = _driveService.Files.List();
                request.Q = "mimeType contains 'image/' and trashed=false";
                request.Fields = "files(id,name,mimeType)";
                request.PageSize = 5;
                
                var response = await request.ExecuteAsync();

                return new BasicAccessInfo
                {
                    HasAccess = true,
                    SampleFilesFound = response.Files?.Count ?? 0,
                    Message = $"Drive OK ({response.Files?.Count ?? 0} images)"
                };
            }
            catch (Exception ex)
            {
                return new BasicAccessInfo
                {
                    HasAccess = false,
                    SampleFilesFound = 0,
                    Message = $"Drive failed: {ex.Message}"
                };
            }
        }

        private async Task<BasicAccessInfo> TestPhotosAccessAsync()
        {
            try
            {
                var searchRequest = new SearchMediaItemsRequest
                {
                    PageSize = 5
                };
                
                var response = await _photosService.MediaItems.Search(searchRequest).ExecuteAsync();
                var photoCount = response.MediaItems?.Count(item => item.MediaMetadata?.Photo != null) ?? 0;

                return new BasicAccessInfo
                {
                    HasAccess = true,
                    SampleFilesFound = photoCount,
                    Message = $"Photos OK ({photoCount} items)"
                };
            }
            catch (Exception ex)
            {
                return new BasicAccessInfo
                {
                    HasAccess = false,
                    SampleFilesFound = 0,
                    Message = $"Photos failed: {ex.Message}"
                };
            }
        }

        public async Task<string> GetPhotoBase64Async(string fileId)
        {
            // Try Drive first, then Photos
            try
            {
                var request = _driveService.Files.Get(fileId);
                var stream = new MemoryStream();
                await request.DownloadAsync(stream);

                var bytes = stream.ToArray();
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                // If Drive fails, try downloading from Photos using the WebContentLink
                // This would require the full URL from the PhotoInfo object
                throw new NotImplementedException("Google Photos direct download not implemented yet");
            }
        }

        // Get photos with custom filtering
        public async Task<List<PhotoInfo>> GetPhotosWithFilterAsync(string? mimeTypeFilter = null, int maxResults = 1000, string? folderName = null)
        {
            // For now, only apply filtering to Drive photos
            // Google Photos API has different filtering mechanisms
            var drivePhotos = await GetDrivePhotosWithFilterAsync(mimeTypeFilter, maxResults / 2, folderName);
            

            var allPhotos = new List<PhotoInfo>();
            allPhotos.AddRange(drivePhotos);

            // Apply client-side filtering for Google Photos if needed
            if (!string.IsNullOrEmpty(mimeTypeFilter))
            {
                allPhotos = allPhotos.Where(p => p.MimeType.Contains(mimeTypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return allPhotos.Take(maxResults).ToList();
        }

        private async Task<List<PhotoInfo>> GetDrivePhotosWithFilterAsync(string? mimeTypeFilter, int maxResults, string? folderName)
        {
            var allPhotos = new List<PhotoInfo>();
            string? nextPageToken = null;

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
                query += $" and parents in (select id from parents where name = '{folderName}')";
            }

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
                                Size = file.Size,
                                Source = "Google Drive"
                            });
                        }
                    }

                    nextPageToken = response.NextPageToken;

                } catch (Exception ex)
                {
                    Console.WriteLine($"❌ Filter search error: {ex.Message}");
                    break;
                }

            } while (!string.IsNullOrEmpty(nextPageToken) && allPhotos.Count < maxResults);

            return allPhotos;
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
        public string Source { get; set; } = string.Empty; // "Google Drive" or "Google Photos"
    }

    public class BasicAccessInfo
    {
        public bool HasAccess { get; set; }
        public int SampleFilesFound { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}