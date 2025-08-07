using Microsoft.AspNetCore.Mvc;
using OllamaFarmer.Server.Data;
using OllamaFarmer.Server.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;
using System.Net.Mime;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController(ILogger<FileController> logger, IBinaryRepository binaryRepository)
        : ControllerBase
    {

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IEnumerable<IFormFile> file, [FromForm] string? path)
        {
            if (file == null)
            {
                return BadRequest("No files uploaded.");
            }

            bool hasFiles = false;
            foreach (var formFile in file)
            {
                hasFiles = true; // Check if there are any files
                if (formFile.Length == 0)
                {
                    continue; // Skip empty files
                }

                var fileName = Path.GetFileName(formFile.FileName);
                // Combine path with filename if path is provided
                var fullPath = !string.IsNullOrEmpty(path) 
                    ? Path.Combine(path.TrimStart('/'), fileName).Replace('\\', '/')
                    : fileName;
                    
                using var stream = formFile.OpenReadStream();
                await binaryRepository.SaveFileAsync(fullPath, stream);
            }

            if (!hasFiles)
            {
                return BadRequest("No files uploaded.");
            }

            return Ok(new { message = "Files uploaded successfully." });
        }

        // Download a file
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (!binaryRepository.FileExists(fileName))
                return NotFound();

            var stream = await binaryRepository.GetFileStreamAsync(fileName);
            if (stream == null)
                return NotFound();

            return File(stream, MapMediaType(fileName), fileName);
        }

        // Return image
        [HttpGet("image/{fileName}")]
        public async Task<IActionResult> Image(string fileName)
        {
            if (!binaryRepository.FileExists(fileName))
                return NotFound();

            var stream = await binaryRepository.GetFileStreamAsync(fileName);
            if (stream == null)
                return NotFound();

            var type = MapMediaType(fileName);

            if(type != MediaTypeNames.Image.Jpeg &&
               type != MediaTypeNames.Image.Png &&
               type != MediaTypeNames.Image.Gif &&
               type != MediaTypeNames.Image.Bmp &&
               type != "image/webp")
            {
                return BadRequest("Unsupported image format.");
            }

            return File(stream, type, fileName);
        }

        private string MapMediaType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => MediaTypeNames.Image.Jpeg,
                ".png" => MediaTypeNames.Image.Png,
                ".gif" => MediaTypeNames.Image.Gif,
                ".bmp" => MediaTypeNames.Image.Bmp,
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        // List all files
        [HttpGet("list/{path}")]
        public async Task<ActionResult<IEnumerable<string>>> List(string? path)
        {
            var files = binaryRepository.ListFiles(path ?? "");
            return Ok(files);
        }

        // List all files
        [HttpGet("list-meta/{path}")]
        public async Task<ActionResult<List<FileMetaDataDto>>> ListMeta(string? path, [FromQuery]bool includeSubDirectories)
        {
            var cleanPath = HttpUtility.UrlDecode(path ?? "/").TrimEnd('/');
            // Handle empty path as root directory
            if (string.IsNullOrEmpty(cleanPath))
                cleanPath = "/";
                
            var files = binaryRepository.ListFilesMeta(cleanPath, includeSubDirectories);
            return Ok(files);
        }

        // Check if a file exists
        [HttpGet("exists/{fileName}")]
        public async Task<ActionResult<bool>> Exists(string fileName)
        {
            var exists = binaryRepository.FileExists(fileName);
            return Ok(exists);
        }

        // Delete a file
        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (!binaryRepository.FileExists(fileName))
                return NotFound();

            binaryRepository.DeleteFile(fileName);
            return NoContent();
        }

        // Move(/rename) a file
        [HttpPut("move/{fileName}")]
        public async Task<IActionResult> Move(string fileName, [FromQuery] string newFileName)
        {
            if (!binaryRepository.FileExists(fileName))
                return NotFound();

            if (string.IsNullOrWhiteSpace(newFileName))
                return BadRequest("New file name is required.");

            await binaryRepository.MoveFileAsync(fileName, newFileName);
            return NoContent();
        }

        // Get file metadata (if supported)
        [HttpGet("metadata/{fileName}")]
        public async Task<ActionResult<FileMetaDataDto?>> Metadata(string fileName)
        {
            var metadata = await binaryRepository.GetMetadataAsync(fileName);
            if (metadata == null)
                return NotFound();

            return Ok(metadata);
        }

        // Upload an image and its thumbnail
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string fileName)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Save original image
            using var stream = file.OpenReadStream();
            await binaryRepository.SaveFileAsync(fileName, stream);

            // Generate and save thumbnail
            stream.Position = 0;
            using var image = System.Drawing.Image.FromStream(file.OpenReadStream());
            int thumbWidth = 128;
            int thumbHeight = (int)(image.Height * (128.0 / image.Width));
            using var thumb = image.GetThumbnailImage(thumbWidth, thumbHeight, () => false, IntPtr.Zero);
            using var ms = new MemoryStream();
            thumb.Save(ms, ImageFormat.Jpeg);
            ms.Position = 0;
            var thumbName = $"{Path.GetFileNameWithoutExtension(fileName)}.thumb.jpg";
            await binaryRepository.SaveFileAsync(thumbName, ms);

            return Ok(new { fileName, thumbnail = thumbName });
        }

        // Download thumbnail
        [HttpGet("thumbnail/{fileName}")]
        public async Task<IActionResult> DownloadThumbnail(string fileName)
        {
            var thumbName = $"{Path.GetFileNameWithoutExtension(fileName)}.thumb.jpg";
            if (!binaryRepository.FileExists(thumbName))
                return NotFound();

            var stream = await binaryRepository.GetFileStreamAsync(thumbName);
            if (stream == null)
                return NotFound();

            return File(stream, MapMediaType(fileName), thumbName);
        }

        // List all thumbnails
        [HttpGet("list-thumbnails/{path}")]
        public ActionResult<IEnumerable<string>> ListThumbnails(string path)
        {
            var thumbs = binaryRepository.ListFiles(path)
                .Where(f => f.EndsWith(".thumb.jpg", StringComparison.OrdinalIgnoreCase));
            return Ok(thumbs);
        }

        [HttpPost("create-directory")]
        public IActionResult CreateDirectory([FromBody] string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
                return BadRequest("Directory name cannot be empty.");
            return Ok(binaryRepository.CreateDirectoryAsync(directoryName));
        }

        // Get image metadata
        [HttpGet("image-metadata/{fileName}")]
        public async Task<IActionResult> ImageMetadata(string fileName)
        {
            if (!binaryRepository.FileExists(fileName))
                return NotFound();

            using var stream = await binaryRepository.GetFileStreamAsync(fileName);
            if (stream == null)
                return NotFound();

            try
            {
                using var image = System.Drawing.Image.FromStream(stream);
                var metadata = new
                {
                    Width = image.Width,
                    Height = image.Height,
                    Format = image.RawFormat.ToString()
                };
                return Ok(metadata);
            }
            catch
            {
                return BadRequest("File is not a valid image.");
            }
        }
    }
}
