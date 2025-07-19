using System.Reflection;
using System.Text;

namespace FunctionApp2.Services
{
    public static class HtmlTemplateService
    {
        public static string LoadTemplate(string templateName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"FunctionApp2.Templates.{templateName}.html";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Template '{templateName}' not found.");
            }
            
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string GeneratePhotoGalleryHtml(List<PhotoInfo> photos)
        {
            var template = LoadTemplate("PhotoGallery");
            
            var photoItems = new StringBuilder();
            
            // Group photos by source for better organization
            var photoGroups = photos.GroupBy(p => p.Source).ToList();
            
            foreach (var group in photoGroups)
            {
                if (photoGroups.Count > 1 && !string.IsNullOrEmpty(group.Key))
                {
                    var sourceIcon = group.Key == "Google Photos" ? "??" : "??";
                    photoItems.AppendLine($@"
                        <div class=""source-header"">
                            <h3>{sourceIcon} {group.Key} ({group.Count()} photo{(group.Count() != 1 ? "s" : "")})</h3>
                        </div>");
                }

                foreach (var photo in group)
                {
                    var sourceTag = !string.IsNullOrEmpty(photo.Source) ? $"<span class=\"source-tag\">{photo.Source}</span>" : "";
                    var sizeInfo = photo.Size.HasValue ? $" ({FormatFileSize(photo.Size.Value)})" : "";
                    var dateInfo = photo.CreatedTime.HasValue ? photo.CreatedTime.Value.ToString("MMM dd, yyyy") : "Unknown date";
                    
                    photoItems.AppendLine($@"
                        <div class=""photo-item"" onclick=""openFullImage('{photo.WebContentLink}', '{photo.Name}')"">
                            <img src=""{photo.ThumbnailLink}"" alt=""{photo.Name}"" title=""{photo.Name}"" loading=""lazy"">
                            <div class=""photo-overlay"">
                                <div class=""photo-name"">{photo.Name}{sizeInfo}</div>
                                <div class=""photo-info"">{dateInfo}</div>
                                {sourceTag}
                                <div class=""photo-actions"">
                                    <button onclick=""event.stopPropagation(); openDriveLink('{photo.WebViewLink}')"" class=""btn-drive"">
                                        ?? View Original
                                    </button>
                                    <button onclick=""event.stopPropagation(); downloadPhoto('{photo.WebContentLink}', '{photo.Name}')"" class=""btn-download"">
                                        ?? Download
                                    </button>
                                </div>
                            </div>
                        </div>");
                }
            }

            var driveCount = photos.Count(p => p.Source == "Google Drive");
            var photosCount = photos.Count(p => p.Source == "Google Photos");
            var sourceBreakdown = "";
            
            if (driveCount > 0 && photosCount > 0)
            {
                sourceBreakdown = $"<div class=\"source-breakdown\">?? Drive: {driveCount} | ?? Photos: {photosCount}</div>";
            }

            var galleryContent = photos.Count > 0 
                ? $@"<div class=""photo-grid"">{photoItems}</div>"
                : @"<div class=""no-photos"">
                    <h2>?? No Photos Found</h2>
                    <p>No images were found in your Google Drive or Google Photos.<br><br>
                    <strong>To see photos in this app:</strong><br>
                    1. <strong>Google Drive:</strong> Upload photos and share with <strong>gphotosapi@linen-synthesis-465818-n3.iam.gserviceaccount.com</strong><br>
                    2. <strong>Google Photos:</strong> Share photos or albums with the service account<br><br>
                    This app searches both sources automatically!</p>
                  </div>";

            return template
                .Replace("{{PHOTO_COUNT}}", photos.Count.ToString())
                .Replace("{{PHOTO_COUNT_PLURAL}}", photos.Count != 1 ? "s" : "")
                .Replace("{{SOURCE_BREAKDOWN}}", sourceBreakdown)
                .Replace("{{GALLERY_CONTENT}}", galleryContent);
        }

        public static string GenerateErrorHtml(string errorMessage)
        {
            var template = LoadTemplate("Error");
            return template.Replace("{{ERROR_MESSAGE}}", errorMessage);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}