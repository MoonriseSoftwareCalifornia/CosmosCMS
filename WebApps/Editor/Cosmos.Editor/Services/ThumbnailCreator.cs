// <copyright file="ThumbnailCreator.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Cosmos.Cms.Models;

namespace Cosmos.Cms.Services
{
    /// <summary>
    /// Image thumbnail creator.
    /// </summary>
    public class ThumbnailCreator
    {
        private static readonly IDictionary<string, ImageFormat> ImageFormats = new Dictionary<string, ImageFormat>
        {
            { "image/png", ImageFormat.Png },
            { "image/gif", ImageFormat.Gif },
            { "image/jpeg", ImageFormat.Jpeg }
        };

        private readonly ImageResizer resizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThumbnailCreator"/> class.
        /// Constructor.
        /// </summary>
        public ThumbnailCreator()
        {
            resizer = new ImageResizer();
        }

        /// <summary>
        /// Create thumbnail.
        /// </summary>
        /// <param name="source">Source stream data.</param>
        /// <param name="desiredSize">Desired size of image.</param>
        /// <param name="contentType">MIME type of image.</param>
        /// <returns>Byte array.</returns>
        public byte[] Create(Stream source, ImageSizeModel desiredSize, string contentType)
        {
            using (var image = Image.FromStream(source))
            {
                var originalSize = new ImageSizeModel
                {
                    Height = image.Height,
                    Width = image.Width
                };

                var size = resizer.Resize(originalSize, desiredSize);

                using (var thumbnail = new Bitmap(size.Width, size.Height))
                {
                    ScaleImage(image, thumbnail);

                    using (var memoryStream = new MemoryStream())
                    {
                        thumbnail.Save(memoryStream, ImageFormats[contentType]);

                        return memoryStream.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Create filkl.
        /// </summary>
        /// <param name="source">Source data stream.</param>
        /// <param name="desiredSize">Desired image size.</param>
        /// <param name="contentType">MIME content type.</param>
        /// <returns>Byte array.</returns>
        public byte[] CreateFill(Stream source, ImageSizeModel desiredSize, string contentType)
        {
            using (var image = Image.FromStream(source))
            {
                using (var memoryStream = new MemoryStream())
                {
                    FixedSize(image, desiredSize.Width, desiredSize.Height, true)
                        .Save(memoryStream, ImageFormats[contentType]);
                    return memoryStream.ToArray();
                }
            }
        }

        private void ScaleImage(Image source, Image destination)
        {
            using (var graphics = Graphics.FromImage(destination))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                graphics.DrawImage(source, 0, 0, destination.Width, destination.Height);
            }
        }

        private Image FixedSize(Image imgPhoto, int width, int height, bool needToFill)
        {
            var sourceWidth = imgPhoto.Width;
            var sourceHeight = imgPhoto.Height;
            var sourceX = 0;
            var sourceY = 0;
            var destX = 0;
            var destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = width / (float)sourceWidth;
            nPercentH = height / (float)sourceHeight;
            if (!needToFill)
            {
                if (nPercentH < nPercentW)
                {
                    nPercent = nPercentH;
                }
                else
                {
                    nPercent = nPercentW;
                }
            }
            else
            {
                if (nPercentH > nPercentW)
                {
                    nPercent = nPercentH;
                    destX = (int)Math.Round((width - 
                                              sourceWidth * nPercent) / 2);
                }
                else
                {
                    nPercent = nPercentW;
                    destY = (int)Math.Round((height -
                                              sourceHeight * nPercent) / 2);
                }
            }

            if (nPercent > 1)
            {
                nPercent = 1;
            }

            var destWidth = (int)Math.Round(sourceWidth * nPercent);
            var destHeight = (int)Math.Round(sourceHeight * nPercent);

            var bmPhoto = new Bitmap(
                destWidth <= width ? destWidth : width,
                destHeight < height ? destHeight : height,
                PixelFormat.Format32bppRgb);

            var grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grPhoto.CompositingQuality = CompositingQuality.HighQuality;
            grPhoto.SmoothingMode = SmoothingMode.AntiAlias;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }
    }
}