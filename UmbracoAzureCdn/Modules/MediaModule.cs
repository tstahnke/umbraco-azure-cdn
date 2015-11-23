using System;
using System.Configuration;
using System.Web;
using UmbracoAzureCdn.Helpers;
using UmbracoAzureCdn.Models;

public class MediaModule : IHttpModule
{
    public MediaModule()
    {
    }

    // In the Init function, register for HttpApplication 
    // events by adding your handlers.
    public void Init(HttpApplication application)
    {
        application.BeginRequest +=
            (new EventHandler(this.Application_BeginRequest));
    }

    private void RetrieveFileData(HttpContext context, string filePath, string container)
    {
        MediaFileModel resultModel = new MediaFileModel();
        // only send request to imageprocessor if querystring exists; can exclude other parameters if needed
        if (context.Request.RawUrl.Contains("?"))
        {
            resultModel = MediaHelper.GetMediaFile(filePath, container, context.Request.RawUrl);
        }
        else
        {
            resultModel = MediaHelper.GetMediaFile(filePath, container);
        }
        if (resultModel.RedirectToAzureStorage)
        {
            context.Response.Redirect(filePath.Replace($"/{container}", $"{ConfigurationManager.AppSettings["BlobStorage"]}/{container}"), true);
        }
        var myTimeSpan = new TimeSpan(7, 0, 0, 0);
        context.Response.Cache.SetCacheability(HttpCacheability.Public);
        context.Response.Cache.SetValidUntilExpires(true);
        context.Response.Cache.SetMaxAge(myTimeSpan);
        context.Response.Cache.SetLastModified(resultModel.LastModifiedDate);
        context.Response.Cache.SetETag(resultModel.ETag.Replace("\\\"", ""));
        context.Response.AddHeader("Content-MD5", resultModel.ContentMd5);
        context.Response.ContentType = resultModel.ContentType;
        // replicate properties returned by blob storage
        context.Response.AddHeader("Server", "Windows-Azure-Blob/1.0 Microsoft-HTTPAPI/2.0");
        context.Response.AddHeader("x-ms-request-id", Guid.NewGuid().ToString());
        context.Response.AddHeader("x-ms-version", "2009-09-19");
        context.Response.AddHeader("x-ms-lease-status", "unlocked");
        context.Response.AddHeader("x-ms-blob-type", "BlockBlob");
        context.Response.OutputStream.Write(resultModel.ImageData, 0, resultModel.ImageData.Length);
        context.Response.AddHeader("Content-Length", resultModel.ImageData.Length.ToString());
        context.Response.Flush();
        context.Response.End();
    }

    private void Application_BeginRequest(Object source,
         EventArgs e)
    {
        // Create HttpApplication and HttpContext objects to access
        // request and response properties.
        HttpApplication application = (HttpApplication)source;
        HttpContext context = application.Context;
        string filePath = context.Request.FilePath;
        if (filePath.ToLower().StartsWith("/media"))
        {
            RetrieveFileData(context, filePath, "media");
        }
    }

    public void Dispose() { }
}