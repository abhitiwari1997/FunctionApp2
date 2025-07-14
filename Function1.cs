using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace FunctionApp2
{
    internal class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("GetPhotos")]
        public async Task<IActionResult> GetPhotos([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");
                if (string.IsNullOrEmpty(credentialsJson))
                {
                    return new BadRequestObjectResult("Google credentials not configured");
                }

                var driveService = new GoogleDrivePhotoService(credentialsJson);
                var photos = await driveService.GetPhotosAsync(10);

                // Generate HTML content with the photos
                var htmlContent = GeneratePhotoGalleryHtml(photos);

                return new ContentResult
                {
                    Content = htmlContent,
                    ContentType = "text/html",
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching photos from Google Drive");

                // Return error HTML page
                var errorHtml = GenerateErrorHtml(ex.Message);
                return new ContentResult
                {
                    Content = errorHtml,
                    ContentType = "text/html",
                    StatusCode = 500
                };
            }
        }

        private string GeneratePhotoGalleryHtml(List<PhotoInfo> photos)
        {
            var photoItems = new StringBuilder();

            foreach (var photo in photos)
            {
                photoItems.AppendLine($@"
                    <div class=""photo-item"" onclick=""openFullImage('{photo.WebContentLink}', '{photo.Name}')"">
                        <img src=""{photo.ThumbnailLink}"" alt=""{photo.Name}"" title=""{photo.Name}"" loading=""lazy"">
                        <div class=""photo-overlay"">
                            <div class=""photo-name"">{photo.Name}</div>
                            <div class=""photo-actions"">
                                <button onclick=""event.stopPropagation(); openDriveLink('{photo.WebViewLink}')"" class=""btn-drive"">
                                    📁 View in Drive
                                </button>
                                <button onclick=""event.stopPropagation(); downloadPhoto('{photo.WebContentLink}', '{photo.Name}')"" class=""btn-download"">
                                    💾 Download
                                </button>
                            </div>
                        </div>
                    </div>");
            }

            return $@"<!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Photo Gallery - Abhishek Tiwari</title>
                <style>
                    * {{
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }}

                    body {{
                        font-family: 'Arial', sans-serif;
                        background: linear-gradient(45deg, #1e3c72, #2a5298, #6dd5ed, #2193b0);
                        background-size: 400% 400%;
                        animation: gradientShift 8s ease infinite;
                        min-height: 100vh;
                        color: white;
                    }}

                    @keyframes gradientShift {{
                        0% {{ background-position: 0% 50%; }}
                        50% {{ background-position: 100% 50%; }}
                        100% {{ background-position: 0% 50%; }}
                    }}

                    .header {{
                        text-align: center;
                        padding: 40px 20px;
                        background: rgba(0, 0, 0, 0.2);
                        backdrop-filter: blur(10px);
                        margin-bottom: 40px;
                    }}

                    .header h1 {{
                        font-size: 3rem;
                        margin-bottom: 10px;
                        text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
                        animation: pulse 2s ease-in-out infinite;
                    }}

                    @keyframes pulse {{
                        0%, 100% {{ transform: scale(1); }}
                        50% {{ transform: scale(1.05); }}
                    }}

                    .header p {{
                        font-size: 1.2rem;
                        opacity: 0.9;
                    }}

                    .photo-count {{
                        background: rgba(255, 255, 255, 0.2);
                        padding: 10px 20px;
                        border-radius: 25px;
                        display: inline-block;
                        margin-top: 20px;
                        font-weight: bold;
                    }}

                    .gallery-container {{
                        max-width: 1200px;
                        margin: 0 auto;
                        padding: 0 20px;
                    }}

                    .photo-grid {{
                        display: grid;
                        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
                        gap: 30px;
                        margin-bottom: 40px;
                    }}

                    .photo-item {{
                        position: relative;
                        background: rgba(255, 255, 255, 0.1);
                        border-radius: 15px;
                        overflow: hidden;
                        cursor: pointer;
                        transition: all 0.3s ease;
                        backdrop-filter: blur(10px);
                        border: 1px solid rgba(255, 255, 255, 0.2);
                    }}

                    .photo-item:hover {{
                        transform: translateY(-10px) scale(1.02);
                        box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
                        border-color: rgba(255, 255, 255, 0.4);
                    }}

                    .photo-item img {{
                        width: 100%;
                        height: 250px;
                        object-fit: cover;
                        transition: transform 0.3s ease;
                    }}

                    .photo-item:hover img {{
                        transform: scale(1.1);
                    }}

                    .photo-overlay {{
                        position: absolute;
                        bottom: 0;
                        left: 0;
                        right: 0;
                        background: linear-gradient(transparent, rgba(0, 0, 0, 0.8));
                        color: white;
                        padding: 20px;
                        transform: translateY(70%);
                        transition: transform 0.3s ease;
                    }}

                    .photo-item:hover .photo-overlay {{
                        transform: translateY(0);
                    }}

                    .photo-name {{
                        font-weight: bold;
                        font-size: 1.1rem;
                        margin-bottom: 10px;
                        text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.7);
                    }}

                    .photo-actions {{
                        display: flex;
                        gap: 10px;
                        margin-top: 10px;
                    }}

                    .btn-drive, .btn-download {{
                        background: rgba(255, 255, 255, 0.2);
                        border: 1px solid rgba(255, 255, 255, 0.3);
                        color: white;
                        padding: 8px 12px;
                        border-radius: 20px;
                        cursor: pointer;
                        font-size: 0.9rem;
                        transition: all 0.3s ease;
                        backdrop-filter: blur(10px);
                    }}

                    .btn-drive:hover, .btn-download:hover {{
                        background: rgba(255, 255, 255, 0.3);
                        transform: scale(1.05);
                    }}

                    .no-photos {{
                        text-align: center;
                        padding: 60px 20px;
                        background: rgba(255, 255, 255, 0.1);
                        border-radius: 15px;
                        backdrop-filter: blur(10px);
                    }}

                    .no-photos h2 {{
                        font-size: 2rem;
                        margin-bottom: 20px;
                        opacity: 0.8;
                    }}

                    .no-photos p {{
                        font-size: 1.2rem;
                        opacity: 0.7;
                        line-height: 1.6;
                    }}

                    /* Modal styles */
                    .modal {{
                        display: none;
                        position: fixed;
                        z-index: 1000;
                        left: 0;
                        top: 0;
                        width: 100%;
                        height: 100%;
                        background: rgba(0, 0, 0, 0.9);
                        backdrop-filter: blur(10px);
                    }}

                    .modal-content {{
                        position: relative;
                        margin: auto;
                        display: block;
                        width: 90%;
                        max-width: 900px;
                        max-height: 90vh;
                        object-fit: contain;
                        margin-top: 5vh;
                        border-radius: 10px;
                    }}

                    .close {{
                        position: absolute;
                        top: 20px;
                        right: 30px;
                        color: white;
                        font-size: 40px;
                        font-weight: bold;
                        cursor: pointer;
                        z-index: 1001;
                    }}

                    .close:hover {{
                        opacity: 0.7;
                    }}

                    .modal-title {{
                        position: absolute;
                        bottom: 20px;
                        left: 20px;
                        color: white;
                        font-size: 1.5rem;
                        text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.8);
                        z-index: 1001;
                    }}

                    @media (max-width: 768px) {{
                        .header h1 {{
                            font-size: 2rem;
                        }}
                        
                        .photo-grid {{
                            grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
                            gap: 20px;
                        }}
                        
                        .photo-actions {{
                            flex-direction: column;
                        }}
                    }}
                </style>
            </head>
            <body>
                <div class=""header"">
                    <h1>📸 Photo Gallery</h1>
                    <p>Google Drive Photos - Abhishek Tiwari</p>
                    <div class=""photo-count"">
                        {photos.Count} Photo{(photos.Count != 1 ? "s" : "")} Found
                    </div>
                </div>

                <div class=""gallery-container"">
                    {(photos.Count > 0 ? $@"
                        <div class=""photo-grid"">
                            {photoItems}
                        </div>
                    " : @"
                        <div class=""no-photos"">
                            <h2>📁 No Photos Found</h2>
                            <p>No images were found in your Google Drive.<br>
                            Make sure you have shared some image files with the service account:<br>
                            <strong>gphotosapi@linen-synthesis-465818-n3.iam.gserviceaccount.com</strong></p>
                        </div>
                    ")}
                </div>

                <!-- Modal for full-size image -->
                <div id=""imageModal"" class=""modal"">
                    <span class=""close"">&times;</span>
                    <img class=""modal-content"" id=""modalImage"">
                    <div class=""modal-title"" id=""modalTitle""></div>
                </div>

                <script>
                    function openFullImage(imageUrl, imageName) {{
                        const modal = document.getElementById('imageModal');
                        const modalImg = document.getElementById('modalImage');
                        const modalTitle = document.getElementById('modalTitle');
                        
                        modal.style.display = 'block';
                        modalImg.src = imageUrl;
                        modalTitle.textContent = imageName;
                    }}

                    function openDriveLink(driveUrl) {{
                        window.open(driveUrl, '_blank');
                    }}

                    function downloadPhoto(downloadUrl, fileName) {{
                        const link = document.createElement('a');
                        link.href = downloadUrl;
                        link.download = fileName;
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                    }}

                    // Close modal when clicking on X or outside the image
                    const modal = document.getElementById('imageModal');
                    const span = document.getElementsByClassName('close')[0];

                    span.onclick = function() {{
                        modal.style.display = 'none';
                    }}

                    modal.onclick = function(event) {{
                        if (event.target === modal) {{
                            modal.style.display = 'none';
                        }}
                    }}

                    // Close modal with Escape key
                    document.addEventListener('keydown', function(event) {{
                        if (event.key === 'Escape') {{
                            modal.style.display = 'none';
                        }}
                    }});
                </script>
            </body>
            </html>";
        }

        private string GenerateErrorHtml(string errorMessage)
        {
            return $@"<!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Error - Photo Gallery</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background: linear-gradient(45deg, #ff6b6b, #ee5a52);
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        margin: 0;
                        color: white;
                    }}
                    .error-container {{
                        text-align: center;
                        background: rgba(0, 0, 0, 0.2);
                        padding: 40px;
                        border-radius: 15px;
                        backdrop-filter: blur(10px);
                        max-width: 500px;
                    }}
                    h1 {{ font-size: 3rem; margin-bottom: 20px; }}
                    p {{ font-size: 1.2rem; line-height: 1.6; }}
                    .error-code {{ opacity: 0.8; margin-top: 20px; font-style: italic; }}
                </style>
            </head>
            <body>
                <div class=""error-container"">
                    <h1>❌ Error</h1>
                    <p>Failed to load photos from Google Drive.</p>
                    <div class=""error-code"">Error: {errorMessage}</div>
                </div>
            </body>
            </html>";
        }
    }
}