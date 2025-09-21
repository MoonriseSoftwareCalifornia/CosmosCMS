// <copyright file="ImageResizer.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Services
{
    using System;
    using Sky.Cms.Models;

    /// <summary>
    /// Image resize utility.
    /// </summary>
    public class ImageResizer
    {
        /// <summary>
        /// Calculate image resize.
        /// </summary>
        /// <param name="originalSize"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public ImageSizeModel Resize(ImageSizeModel originalSize, ImageSizeModel targetSize)
        {
            var aspectRatio = originalSize.Width / (float)originalSize.Height;
            var width = targetSize.Width;
            var height = targetSize.Height;

            if (originalSize.Width > targetSize.Width || originalSize.Height > targetSize.Height)
            {
                if (aspectRatio > 1)
                {
                    height = (int)(targetSize.Height / aspectRatio);
                }
                else
                {
                    width = (int)(targetSize.Width * aspectRatio);
                }
            }
            else
            {
                width = originalSize.Width;
                height = originalSize.Height;
            }

            return new ImageSizeModel
            {
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1)
            };
        }
    }
}