﻿using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.IO;
using Umbraco.Core.Media;
using Umbraco.Core.Models;
using Umbraco.Web.Common.Attributes;
using Umbraco.Web.Models;

namespace Umbraco.Web.BackOffice.Controllers
{
    /// <summary>
    /// A controller used to return images for media
    /// </summary>
    [PluginController("UmbracoApi")]
    public class ImagesController : UmbracoAuthorizedApiController
    {
        private readonly IMediaFileSystem _mediaFileSystem;
        private readonly IContentSettings _contentSettings;
        private readonly IImageUrlGenerator _imageUrlGenerator;

        public ImagesController(IMediaFileSystem mediaFileSystem, IContentSettings contentSettings, IImageUrlGenerator imageUrlGenerator)
        {
            _mediaFileSystem = mediaFileSystem;
            _contentSettings = contentSettings;
            _imageUrlGenerator = imageUrlGenerator;
        }

        /// <summary>
        /// Gets the big thumbnail image for the original image path
        /// </summary>
        /// <param name="originalImagePath"></param>
        /// <returns></returns>
        /// <remarks>
        /// If there is no original image is found then this will return not found.
        /// </remarks>
        public IActionResult GetBigThumbnail(string originalImagePath)
        {
            return string.IsNullOrWhiteSpace(originalImagePath)
                ? Ok()
                : GetResized(originalImagePath, 500);
        }

        /// <summary>
        /// Gets a resized image for the image at the given path
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        /// <remarks>
        /// If there is no media, image property or image file is found then this will return not found.
        /// </remarks>
        public IActionResult GetResized(string imagePath, int width)
        {
            var ext = Path.GetExtension(imagePath);

            // we need to check if it is an image by extension
            if (_contentSettings.IsImageFile(ext) == false)
                return NotFound();

            //redirect to ImageProcessor thumbnail with rnd generated from last modified time of original media file
            DateTimeOffset? imageLastModified = null;
            try
            {
                imageLastModified = _mediaFileSystem.GetLastModified(imagePath);

            }
            catch (Exception)
            {
                // if we get an exception here it's probably because the image path being requested is an image that doesn't exist
                // in the local media file system. This can happen if someone is storing an absolute path to an image online, which
                // is perfectly legal but in that case the media file system isn't going to resolve it.
                // so ignore and we won't set a last modified date.
            }

            var rnd = imageLastModified.HasValue ? $"&rnd={imageLastModified:yyyyMMddHHmmss}" : null;
            var imageUrl = _imageUrlGenerator.GetImageUrl(new ImageUrlGenerationOptions(imagePath) { UpScale = false, Width = width, AnimationProcessMode = "first", ImageCropMode = ImageCropMode.Max, CacheBusterValue = rnd });

            return new RedirectResult(imageUrl, false);
        }

        /// <summary>
        /// Gets a processed image for the image at the given path
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="focalPointLeft"></param>
        /// <param name="focalPointTop"></param>
        /// <param name="animationProcessMode"></param>
        /// <param name="mode"></param>
        /// <param name="upscale"></param>
        /// <returns></returns>
        /// <remarks>
        /// If there is no media, image property or image file is found then this will return not found.
        /// </remarks>
        public string GetProcessedImageUrl(string imagePath,
                                           int? width = null,
                                           int? height = null,
                                           int? focalPointLeft = null,
                                           int? focalPointTop = null,
                                           string animationProcessMode = "first",
                                           ImageCropMode mode = ImageCropMode.Max,
                                           bool upscale = false,
                                           string cacheBusterValue = "")
{
            var options = new ImageUrlGenerationOptions(imagePath)
            {
                AnimationProcessMode = animationProcessMode,
                CacheBusterValue = cacheBusterValue,
                Height = height,
                ImageCropMode = mode,
                UpScale = upscale,
                Width = width,
            };
            if (focalPointLeft.HasValue && focalPointTop.HasValue)
            {
                options.FocalPoint = new ImageUrlGenerationOptions.FocalPointPosition(focalPointTop.Value, focalPointLeft.Value);
            }

            return _imageUrlGenerator.GetImageUrl(options);
        }
    }
}