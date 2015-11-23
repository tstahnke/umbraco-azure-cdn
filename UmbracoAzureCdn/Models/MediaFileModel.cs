using System;

namespace UmbracoAzureCdn.Models
{
    public class MediaFileModel
    {
        public byte[] ImageData { get; set; }
        public string ContentType { get; set; }
        public bool RedirectToAzureStorage { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string ETag { get; set; }
        public string ContentMd5 { get; set; }
    }
}