using System;
using System.Configuration;
using System.IO;
using System.Net;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using UmbracoAzureCdn.Models;

namespace UmbracoAzureCdn.Helpers
{
    public class MediaHelper
    {
        private static CloudStorageAccount Account => CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureWebStorage"));

        /// <summary>
        /// Retrieve media content directly from blob storage.
        /// </summary>
        /// <param name="filePath">Relative file path</param>
        /// <param name="container">Container to retrieve media file from</param>
        /// <returns>Media file using the MediaFileModel</returns>
        public static MediaFileModel GetMediaFile(string filePath, string container)
        {
            var myModel = new MediaFileModel();

            var storageAccount = Account;
            // validate the container has been defined
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentNullException("Container is missing; this is required.");
            var blobContainer = storageAccount.CreateCloudBlobClient().GetContainerReference(container);

            // validate the filepath isn't empty
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException("File path is missing; this is required.");
            var mediaFile = blobContainer.GetBlockBlobReference(filePath.ToLower().Replace($"/{container}/", string.Empty));

            // validate the file exists; if not, return 404
            if (mediaFile == null || !mediaFile.Exists())
            {
                myModel.RedirectToAzureStorage = true;
                return myModel;
            }
            var myStream = new MemoryStream();
            mediaFile.DownloadToStream(myStream);
            myModel.ImageData = myStream.ToArray();
            myModel.ContentType = mediaFile.Properties.ContentType;
            myModel.RedirectToAzureStorage = false;
            myModel.LastModifiedDate = mediaFile.Properties.LastModified?.DateTime ?? DateTime.Now;
            myModel.ETag = mediaFile.Properties.ETag;
            myModel.ContentMd5 = mediaFile.Properties.ContentMD5;
            return myModel;
        }

        /// <summary>
        /// Retrieve media file from Azure Storage and additionally call image processor to retrieve the dynamic image based on the raw url.
        /// </summary>
        /// <param name="filePath">The relative filepath to the file.</param>
        /// <param name="container">Container to retrieve media file from.</param>
        /// <param name="rawUrl">The url to the file including the query string.</param>
        /// <returns>Media file using the MediaFileModel</returns>
        public static MediaFileModel GetMediaFile(string filePath, string container, string rawUrl)
        {
            var myModel = new MediaFileModel();

            myModel = GetMediaFile(filePath, container);
            // if RedirectToAzureStorage was set, we couldn't retrieve the media file, so doubtful the remote request will work either.
            if (myModel.RedirectToAzureStorage) return myModel;

            // call remote.axd to the defined Azure CDN and pass in the blob storage location.
            var webc = new WebClient();
            myModel.ImageData =
                webc.DownloadData(
                    $"{ConfigurationManager.AppSettings["AzureCDN"]}/remote.axd/{ConfigurationManager.AppSettings["BlobStorageRequest"]}{rawUrl}");
            return myModel;
        }
    }
}