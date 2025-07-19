using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using FunctionApp2.Services;

namespace FunctionApp2
{
    internal class Function1
    {
        private readonly ILogger<Function1> _logger;

        // Hardcoded credentials for testing (remove from production!)
        private const string GOOGLE_CREDENTIALS_JSON = @"{
            ""type"": ""service_account"",
            ""project_id"": ""linen-synthesis-465818-n3"",
            ""private_key_id"": ""6c0071323441ccb87dba6f24d004af24c177bb6c"",
            ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDA1sAgxAMr0HvV\nUXpMw++yz4VWIb1G/zIONbHZMnBxsSfA8ESJIwU5yQKpfXNQbqjMHEizbSBGsxOX\nnXh1UZaIbZLhlib1CQM6LRg/URUQXAdohU3+mnL5b+E+G8ixg0+VQtekOUQW4q+f\ngM9UBpFYTEgqCBOmN/3aeL58r5x7DNsQaJFKxvwJCEHg9KK7Y/6sfiinerZLDoHl\nIO4rsuUrwPKQyfRVZTSVDh3AXlyMvnL64LL61wUqhPRsCTCn4jBwSqS8jZKK9oXC\nILg0Qw1z57ppU4KB7X9POeyyddtodT0F4ZJzrbuJTodX0E8nXm3zNyOqvnxkFK/Z\ntxwzAqmhAgMBAAECggEAB9XyEqLEWjhrf7yC0rG3gLWL/nb6gPMHrqoh4uh4xzXi\nnqVks4nXwYRYdlihoegNNdYEYj4R7K2EI0oDxgidrEd/i2kVhilqlyeT76a0y2hh\n6K5Z//l4qIgSR/rLCeODDVac+pBVIvTtG/cY9Zoat9LJr+OKINvvlbwISXq9Sgam\nnJExMjJe1on4lZqJXTxmMsjQcUBumwbbRRyEYGBqiWAoswDb3403hgvrUu1TENTD\nkG3xrNuFqKBIe3yOO/3tfi56fGmsB5XZ1sLhE/MWT4Q/AoNEZqo1/o8hZPXGKP5M\nuKo5tWnHOXus9SuXVPGaKOhObugZf/AK7NhDNk6DQQKBgQDshxsvJk97ItigbgOT\nWKdMgKuEMWNe5zJWHTpE7ZZD0hkr6JgZ+m3MJJeAUtPxHvx3jqMsvQty5UQJ790h\ncR8NXyBjOk3BNjVAx/iDRlgLJS8Mj5qLPvRD8Sfo5TKhN2nVyAgzcHPpQnEEPnps\nIh19ZBhFYvz+vDecGoF5K+q8SQKBgQDQtuNGtRdeNEOEpnT04AUjTRVWG3NahLFd\nGYmnhcAQNexoPVaWWbVDBMCyfuDX2dOh57io3jiJH6vmkNY9fzr3mtQ/4o7880nV\nHdlk0dTfpFJYBxH+F/GS90sGlLh/PKHgqPIKjBI/8EKBMO5mellY6Nz8KCxZy3n6\nYPtgEKMSmQKBgAUMkUj9YV74jHVIQ+1GTDP23zJwN3XUK5/o+dB03etOtdjZGz4a\nuXNNKKrFmd6g0bTfp54R3wex2zT3GNpY5tfLOw7DNNu2A4cBfc2Xl9ONFKcI/byR\nOHem0zpGgkEsxKaaoYovkVneYDk9+DEMvWJq25XHmiz56Zn8et0SUe15AoGBAIKI\nn4RtZfwI++FOqf8szInTf/CmonKOYs8zVaBsSj7ZOs3G7wyBdpg/tLTuHXliRrYP\n0rHqqbk5Ea3WD+fOVvbc4rpB2+Pf1OFxFbG0ekqU3tsnMN2V5ARinY20Hd+V8Dgt\n8ZMfH5rVjQJ3s+Jrys2MdglOps5SMCuj1BO01AihAoGBALAYU9Nzni4dZjVh0L5L\nYhyG+/Jr3oU0+moHgpqvr6In2zYgRua43VMsIDCArMCE+PDECBv6IH1RNStmU16C\nvZeye627dI2tyRVdX46mUeWWTRy9hE34tSnyHM3sDUohyDpIkbl1NdF1sQ+a8p/c\ntUWkyaqi2V5aKFtjk1NqcUwM\n-----END PRIVATE KEY-----\n"",
            ""client_email"": ""gphotosapi@linen-synthesis-465818-n3.iam.gserviceaccount.com"",
            ""client_id"": ""116939972104677774983"",
            ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
            ""token_uri"": ""https://oauth2.googleapis.com/token"",
            ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
            ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/gphotosapi%40linen-synthesis-465818-n3.iam.gserviceaccount.com"",
            ""universe_domain"": ""googleapis.com""
        }";

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("GetPhotos")]
        public async Task<IActionResult> GetPhotos([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("Starting combined Google Drive + Google Photos fetch");

                var driveService = new GoogleDrivePhotoService(GOOGLE_CREDENTIALS_JSON);
                
                // Parse limit from query parameter, default to 1000 for testing
                var limit = int.TryParse(req.Query["limit"], out var l) ? l : 1000;
                
                _logger.LogInformation($"Fetching up to {limit} photos from both Drive and Photos...");

                // This will now fetch from both Google Drive AND Google Photos
                var photos = await driveService.GetPhotosAsync(limit);

                _logger.LogInformation($"Successfully retrieved {photos.Count} photos from combined sources");

                // Generate HTML content using template
                var htmlContent = HtmlTemplateService.GeneratePhotoGalleryHtml(photos);

                return new ContentResult
                {
                    Content = htmlContent,
                    ContentType = "text/html",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching photos: {ErrorMessage}", ex.Message);

                // Return error HTML page using template
                var errorHtml = HtmlTemplateService.GenerateErrorHtml(ex.Message);
                return new ContentResult
                {
                    Content = errorHtml,
                    ContentType = "text/html",
                    StatusCode = 500
                };
            }
        }

        // Test access to both Drive and Photos
        [Function("TestAccess")]
        public async Task<IActionResult> TestAccess([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                var driveService = new GoogleDrivePhotoService(GOOGLE_CREDENTIALS_JSON);
                var accessInfo = await driveService.TestAccessAsync();

                return new OkObjectResult(new
                {
                    hasAccess = accessInfo.HasAccess,
                    sampleFilesFound = accessInfo.SampleFilesFound,
                    message = accessInfo.Message
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { error = ex.Message });
            }
        }

        // Filtered search across both Drive and Photos
        [Function("GetPhotosFiltered")]
        public async Task<IActionResult> GetPhotosFiltered([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                var driveService = new GoogleDrivePhotoService(GOOGLE_CREDENTIALS_JSON);
                
                var limit = int.TryParse(req.Query["limit"], out var l) ? l : 1000;
                var mimeType = req.Query["mimeType"].ToString();
                var folder = req.Query["folder"].ToString();

                _logger.LogInformation($"Filtered search across both sources: limit={limit}, mimeType={mimeType}, folder={folder}");

                var photos = await driveService.GetPhotosWithFilterAsync(
                    string.IsNullOrEmpty(mimeType) ? null : mimeType,
                    limit,
                    string.IsNullOrEmpty(folder) ? null : folder
                );

                return new OkObjectResult(new
                {
                    totalFound = photos.Count,
                    drivePhotos = photos.Count(p => p.Source == "Google Drive"),
                    googlePhotos = photos.Count(p => p.Source == "Google Photos"),
                    photos = photos.Take(10).Select(p => new // Return first 10 for API response
                    {
                        id = p.Id,
                        name = p.Name,
                        mimeType = p.MimeType,
                        createdTime = p.CreatedTime,
                        size = p.Size,
                        source = p.Source
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in filtered search: {ErrorMessage}", ex.Message);
                return new BadRequestObjectResult(new { error = ex.Message });
            }
        }
    }
}