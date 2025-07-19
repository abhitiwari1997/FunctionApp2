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
            foreach (var photo in photos)
            {
                photoItems.AppendLine($@"
                    <div class=""photo-item"" onclick=""openFullImage('{photo.WebContentLink}', '{photo.Name}')"">
                        <img src=""{photo.ThumbnailLink}"" alt=""{photo.Name}"" title=""{photo.Name}"" loading=""lazy"">
                        <div class=""photo-overlay"">
                            <div class=""photo-name"">{photo.Name}</div>
                            <div class=""photo-actions"">
                                <button onclick=""event.stopPropagation(); openDriveLink('{photo.WebViewLink}')"" class=""btn-drive"">
                                    ?? View in Drive
                                </button>
                                <button onclick=""event.stopPropagation(); downloadPhoto('{photo.WebContentLink}', '{photo.Name}')"" class=""btn-download"">
                                    ?? Download
                                </button>
                            </div>
                        </div>
                    </div>");
            }

            var galleryContent = photos.Count > 0 
                ? $@"<div class=""photo-grid"">{photoItems}</div>"
                : @"<div class=""no-photos"">
                    <h2>?? No Photos Found</h2>
                    <p>No images were found in your Google Drive.<br>
                    Make sure you have shared some image files with the service account:<br>
                    <strong>gphotosapi@linen-synthesis-465818-n3.iam.gserviceaccount.com</strong></p>
                  </div>";

            return template
                .Replace("{{PHOTO_COUNT}}", photos.Count.ToString())
                .Replace("{{PHOTO_COUNT_PLURAL}}", photos.Count != 1 ? "s" : "")
                .Replace("{{GALLERY_CONTENT}}", galleryContent);
        }

        public static string GenerateErrorHtml(string errorMessage)
        {
            var template = LoadTemplate("Error");
            return template.Replace("{{ERROR_MESSAGE}}", errorMessage);
        }
    }
}