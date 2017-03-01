/* See the LICENSE.txt file in the root folder for license details. */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;

using Utilities.Core;

namespace Utilities.Internet
{
  public class ImageHandler : IHttpHandler
  {
    private enum ImageSize
    {
      Thumbnail,
      OriginalSize
    }

    private class ImageHandlerException : Exception
    {
      public ImageHandlerException() : base() { }
      public ImageHandlerException(String errorMessage) : base(errorMessage) { }
    }

    private static readonly object _semaphore = new Object();

    #region Implementation of IHttpHandler
    public void ProcessRequest(HttpContext context)
    {
      // Can't use the "using" statement to dispose of imageToReturn because
      // it won't be created until later in this method.  A try/finally
      // works just as well, though.
      Bitmap imageToReturn = null;

      try
      {
        String imageFilename = null;
        String contentType = null;
        ImageFormat imageFormat = null;

        try
        {
          imageFilename = this.GetFullyQualifiedImageFilenameParameter(context.Request.QueryString["imagefilename"], context.Server.MapPath(null));
          ImageSize imagesize = this.GetImageSizeParameter(context.Request.QueryString["imagesize"]);
          Size boundingRectangle = this.GetBoundingRectangle(context.Request.QueryString["width"], context.Request.QueryString["height"]);

          imageToReturn = this.GetImageToReturn(imageFilename, imagesize, boundingRectangle);
          contentType = GraphicsUtils.GetContentTypeFromFileExtension(imageFilename);
          imageFormat = GraphicsUtils.GetImageFormatFromFileExtension(imageFilename);
        }
        catch (Exception ex)
        {
          imageToReturn = this.GetErrorImage(ex.Message);
          contentType = "image/jpeg";
          imageFormat = ImageFormat.Jpeg;
        }

        context.Response.ContentType = contentType;
        imageToReturn.Save(context.Response.OutputStream, imageFormat);
      }
      finally
      {
        if (imageToReturn != null)
          imageToReturn.Dispose();
      }
    }

    public Boolean IsReusable
    {
      // It's OK to reuse this HTTP handler.  The only place where multiple instances
      // could interfere with each other is when writing a thumbnail image to disk.
      // That entire operation is serialized via thread locking.
      get { return true; }
    }
    #endregion

    #region URL Querystring Parameter Parsing Methods.
    private String GetFullyQualifiedImageFilenameParameter(String paramValue, String rootPath)
    {
      if (String.IsNullOrWhiteSpace(paramValue))
        throw new ImageHandlerException("ERROR: The imagefilename parameter was not supplied.");

      String fullyQualifiedImageFilename = Path.Combine(rootPath, paramValue);
      if (!File.Exists(fullyQualifiedImageFilename))
        throw new ImageHandlerException(String.Format("ERROR: The file ('{0}') given in the imagefilename parameter value does not exist.", paramValue));

      // No check is done here to see if the file referred to by paramValue actually contains
      // a valid image.  That is done later as part of the application logic.

      return fullyQualifiedImageFilename;
    }

    private ImageSize GetImageSizeParameter(String paramValue)
    {
      if (String.IsNullOrWhiteSpace(paramValue))
        throw new ImageHandlerException("ERROR: The imagesize parameter was not supplied.");

      if (!Enum.IsDefined(typeof(ImageSize), paramValue))
        throw new ImageHandlerException("ERROR: The imagesize parameter's value was not 'Thumbnail' or 'OriginalSize' (value is case-insensitive).");

      return (ImageSize) Enum.Parse(typeof(ImageSize), paramValue, true /* case-insensitive comparison */);
    }

    private Size GetBoundingRectangle(String widthParam, String heightParam)
    {
      // Min and max for either width or height.
      const Int32 minimumDimension = 50;
      const Int32 maximumDimension = 2000;

      Int32 width;
      Int32.TryParse(widthParam, out width); // A failure to parse widthParam will place zero in width.
      width = Math.Max(width, 0);          // Turn negative width into a zero.
      if (!MathUtils.IsInRange(width, minimumDimension, maximumDimension))
        throw new ImageHandlerException(String.Format("ERROR: The supplied width parameter value ({0}) must be between {1} and {2}.",
          width, minimumDimension, maximumDimension));

      Int32 height;
      Int32.TryParse(heightParam, out height);
      height = Math.Max(height, 0);
      if (!MathUtils.IsInRange(width, minimumDimension, maximumDimension))
        throw new ImageHandlerException(String.Format("ERROR: The supplied height parameter value ({0}) must be between {1} and {2}.",
          height, minimumDimension, maximumDimension));

      if ((width == 0) || (height == 0))
      {
        // If one of the parameters was not supplied, or one was not a number, return a default rectangle.
        return new Size(250, 187); // These values seem to work the best...
      }
      else
      {
        // Both parameters were supplied.
        return new Size(width, height);
      }
    }
    #endregion

    #region Application-specific Instance Methods
    private Bitmap GetImageToReturn(String imageFilename, ImageSize imagesize, Size boundingRectangle)
    {
      // Precondition: imageFilename refers to an existing filename.

      Bitmap originalImage;

      try
      {
        originalImage = new Bitmap(imageFilename);
      }
      catch
      {
        // If an exception was thrown, that means 
        // the given filename does not contain an image.
        // Throw an exception to tell the caller.
        throw new ImageHandlerException(String.Format("ERROR: The file '{0}' does not refer to a valid image file.", imageFilename));
      }

      switch (imagesize)
      {
        case ImageSize.OriginalSize:
          return originalImage;

        case ImageSize.Thumbnail:
          {
            // This HTTP handler can be used my multiple processes.
            // This is the only place in the code where two processes could interfere
            // with each other by trying to write a new thumbnail to the same file
            // at the same time.
            lock (_semaphore)
            {
              Bitmap thumbnail;

              // Does the thumbnail image folder exist?
              String thumbnailFolder = Path.Combine(Path.GetDirectoryName(imageFilename), "Thumbnails");
              Directory.CreateDirectory(thumbnailFolder);

              // Does the thumbnail image exist?
              String thumbnailFilename = Path.Combine(thumbnailFolder, Path.GetFileName(imageFilename));
              if (File.Exists(thumbnailFilename))
              {
                try
                {
                  thumbnail = new Bitmap(thumbnailFilename);
                }
                catch
                {
                  // If there was any problem in creating the thumbnail from
                  // an existing file, assume the file does not contain a valid image.
                  throw new ImageHandlerException(String.Format("ERROR: The file '{0}' does not appear to contain a valid image.", thumbnailFilename));
                }

                // It's possible the caller requested a thumbnail of a different size
                // via the width and height querystring parameters.
                // If they did, create a new thumbnail of the requested size and
                // return that.
                if (thumbnail.Size != boundingRectangle)
                {
                  thumbnail.Dispose();
                  thumbnail = GraphicsUtils.GetResizedImage(originalImage, boundingRectangle);
                  thumbnail.Save(thumbnailFilename);
                }
              }
              else
              {
                thumbnail = GraphicsUtils.GetResizedImage(originalImage, boundingRectangle);
                thumbnail.Save(thumbnailFilename);
              }

              return new Bitmap(thumbnail);
            }
          }

        default:
          throw new ImageHandlerException("Unknown ImageSize enumeration value.");
      }
    }

    private Bitmap GetErrorImage(String errorMessage)
    {
      return GraphicsUtils.GetTextImage(errorMessage, new Font("Arial", 12), Color.White, Color.Red);
    }
    #endregion
  }
}
