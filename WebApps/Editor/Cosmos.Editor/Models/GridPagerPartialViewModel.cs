// <copyright file="GridPagerPartialViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;

    /// <summary>
    /// Grid pager partial view model.
    /// </summary>
    public class GridPagerPartialViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridPagerPartialViewModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="rowCount"></param>
        /// <param name="pageSize"></param>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="getUrl"></param>
        /// <param name="filter"></param>
        /// <param name="selectOne"></param>
        /// <param name="showThumbnails"></param>
        public GridPagerPartialViewModel(int pageNumber, int rowCount, int pageSize, string sortOrder, string currentSort, string getUrl, string filter = "", bool selectOne = false, bool showThumbnails = false)
        {
            // Set values as-is
            RowCount = rowCount;
            PageSize = pageSize;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SortOrder = sortOrder;
            CurrentSort = currentSort;
            GetUrl = getUrl;

            // Calculations
            var at = System.Convert.ToDouble(RowCount) / PageSize;
            PageCount = System.Convert.ToInt32(Math.Ceiling(at));
            PreviousPage = Math.Max(0, PageNumber - 1);
            LastPage = PageCount - 1;
            NextPage = Math.Min(pageNumber + 1, LastPage);
            Filter = filter;
            SelectOne = selectOne;
            ShowThumbnails = showThumbnails;
        }

        /// <summary>
        /// Gets total number of rows.
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// Gets current page number (zero index).
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets number of rows in a page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets previous page.
        /// </summary>
        public int PreviousPage { get; }

        /// <summary>
        /// Gets next page.
        /// </summary>
        public int NextPage { get; }

        /// <summary>
        /// Gets last page.
        /// </summary>
        public int LastPage { get; }

        /// <summary>
        /// Gets page count.
        /// </summary>
        public int PageCount { get; }

        /// <summary>
        /// Gets sort order.
        /// </summary>
        public string SortOrder { get; }

        /// <summary>
        /// Gets current sort item.
        /// </summary>
        public string CurrentSort { get; }

        /// <summary>
        /// Gets get method URL.
        /// </summary>
        public string GetUrl { get; }

        /// <summary>
        /// Gets filter value.
        /// </summary>
        public string Filter { get; }

        /// <summary>
        /// Gets a value indicating whether only allow one file to be selected at a time.
        /// </summary>
        public bool SelectOne { get; }

        /// <summary>
        /// Gets a value indicating whether show image thumbnails.
        /// </summary>
        public bool ShowThumbnails { get; }
    }
}
