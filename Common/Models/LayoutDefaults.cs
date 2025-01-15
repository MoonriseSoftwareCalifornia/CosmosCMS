// <copyright file="LayoutDefaults.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System.Collections.Generic;
    using Cosmos.Common.Data;

    /// <summary>
    ///     Default layouts installed during setup.
    /// </summary>
    public static class LayoutDefaults
    {
        /// <summary>
        ///     This method is used to seed the database with all 12 starter web templates.
        /// </summary>
        /// <returns>Returns <see cref="Layout"/>s as a <see cref="List{T}"/>.</returns>
        public static List<Layout> GetStarterLayouts()
        {
            var colorThemes = new[]
            {
                "Delta", "Eureka", "Mono", "OrangeCounty", "PasoRobles", "Sacramento", "SantaBarbara",
                "SantaCruz", "Shasta", "Sierra", "Trinity"
            };

            var oceanSide = GetOceanside();

            var list = new List<Layout> { oceanSide };
            foreach (var theme in colorThemes)
            {
                var layoutView = new LayoutViewModel(oceanSide);
                var t = layoutView.GetLayout();
                t.LayoutName = t.LayoutName.Replace("Oceanside", theme);
                t.IsDefault = false;
                t.Head = t.Head.Replace("oceanside", theme.ToLower());
                list.Add(t);
            }

            return list;
        }

        /// <summary>
        ///     CA.GOV Oceanside Layout.
        /// </summary>
        /// <returns>Returns a <see cref="Layout"/>.</returns>
        public static Layout GetOceanside()
        {
            return new()
            {
                // Negative ID added to avoid this error while seeding:
                // The seed entity for entity type 'Layout' cannot be added because a non-zero value is required for property 'Id'. Consider providing a negative value to avoid collisions with non-seed data.
                IsDefault = true,
                LayoutName = "California State Web Template",
                Notes =
                    "This layout is based on the State of California Web Template, which <a href=\"https://beta.template.webstandards.ca.gov/\">can be found here</a>.",
                Head =
                    "<link href='https://fonts.googleapis.com/css?family=Asap+Condensed:400,600|Source+Sans+Pro:400,700' rel='stylesheet' type='text/css' />\r\n<link href=\"https://california.azureedge.net/cdt/statetemplate/6.0.2/css/cagov.core.css\" rel=\"stylesheet\" />\r\n<link href=\"https://california.azureedge.net/cdt/statetemplate/6.0.2/css/colorscheme-oceanside.css\" rel=\"stylesheet\" />\r\n<script src=\"https://kendo.cdn.telerik.com/2021.3.1207/js/jquery.min.js\"></script>\r\n<script src=\"https://code.jquery.com/jquery-migrate-3.0.1.min.js\"></script>\r\n",
                BodyHtmlAttributes = string.Empty,
                HtmlHeader =
                    "<div id=\"skip-to-content\"><a href=\"#main-content\">Skip to Main Content</a></div>\r\n<!-- Utility Header -->\r\n<div class=\"utility-header\">\r\n    <div class=\"container\">\r\n        <div class=\"group flex-row\">\r\n            <div class=\"social-media-links\">\r\n                <div class=\"header-cagov-logo\"><a\r\n                        href=\"https://ca.gov\"><span class=\"sr-only\">CA.gov</span><img src=\"https://california.azureedge.net/cdt/statetemplate/5.5.6/images/Ca-Gov-Logo-Gold.svg\" class=\"pos-rel\" alt=\"\" aria-hidden=\"true\" /></a>\r\n                </div>\r\n                <a href=\"/\" class=\"ca-gov-icon-home\"><span class=\"sr-only\">Home</span></a>\r\n                <a href=\"/\" class=\"ca-gov-icon-email\"><span class=\"sr-only\">Email</span></a>\r\n            </div>\r\n            <div class=\"settings-links\">\r\n                <a href=\"/contact.html\"><span class=\"ca-gov-icon-person\" aria-hidden=\"true\"></span> Log In</a>\r\n                <button class=\"btn btn-xs btn-primary\" data-toggle=\"collapse\" data-target=\"#siteSettings\" aria-expanded=\"false\" aria-controls=\"siteSettings\"><span class=\"ca-gov-icon-gear\" aria-hidden=\"true\"></span> Settings</button>\r\n            </div>\r\n        </div>\r\n    </div>\r\n</div>\r\n<!--  Settings Bar -->\r\n<div class=\"site-settings section section-standout collapse collapsed\" role=\"alert\" id=\"siteSettings\">\r\n    <div class=\"container  p-y\">\r\n        <div class=\"btn-group btn-group-justified-sm\" role=\"group\" aria-label=\"contrastMode\">\r\n            <div class=\"btn-group\">\r\n                <button type=\"button\" class=\"btn btn-standout disableHighContrastMode\">Default</button></div>\r\n            <div class=\"btn-group\">\r\n                <button type=\"button\" class=\"btn btn-standout enableHighContrastMode\">High Contrast</button></div>\r\n        </div>\r\n        <div class=\"btn-group\" role=\"group\" aria-label=\"textSizeMode\">\r\n            <div class=\"btn-group\"><button type=\"button\" class=\"btn btn-standout resetTextSize\">Reset</button></div>\r\n            <div class=\"btn-group\">\r\n                <button type=\"button\" class=\"btn btn-standout increaseTextSize\"><span class=\"hidden-xs\">Increase Font Size</span><span class=\"visible-xs\">Font <small class=\"ca-gov-icon-plus-line\"></small></span></button>\r\n            </div>\r\n            <div class=\"btn-group\">\r\n                <button type=\"button\" class=\"btn btn-standout decreaseTextSize\"><span class=\"hidden-xs\">Decrease Font Size</span><span class=\"visible-xs\">Font <small class=\"ca-gov-icon-minus-line\"></small></span></button>\r\n            </div>\r\n        </div>\r\n        <button type=\"button\" class=\"close\" data-toggle=\"collapse\" data-target=\"#siteSettings\" aria-expanded=\"false\" aria-controls=\"siteSettings\" aria-label=\"Close\"><span aria-hidden=\"true\">&times;</span></button>\r\n    </div>\r\n</div>\r\n<!-- Branding -->\r\n<div class=\"branding\">\r\n    <div class=\"header-organization-banner\"><a href=\"/\">\r\n            <img src=\"/images/NET-core-logo.png\" alt=\"App Logo\" />\r\n</a></div>\r\n</div>\r\n<!-- mobile navigation controls -->\r\n<div class=\"mobile-controls\">\r\n    <span class=\"mobile-control-group mobile-header-icons\">\r\n        <!-- Add more mobile controls here. These will be on the right side of the mobile page header section -->\r\n    </span>\r\n    <div class=\"mobile-control-group main-nav-icons\">\r\n        <button class=\"mobile-control toggle-search\">\r\n            <span class=\"ca-gov-icon-search\" aria-hidden=\"true\"></span><span class=\"sr-only\">Search</span>\r\n        </button>\r\n        <button id=\"nav-icon3\" class=\"mobile-control toggle-menu\" aria-expanded=\"false\" aria-controls=\"navigation\" data-toggle=\"collapse\" data-target=\"#navigation\">\r\n            <span></span>\r\n            <span></span>\r\n            <span></span>\r\n            <span></span>\r\n            <span class=\"sr-only\">Menu</span>\r\n        </button>\r\n\r\n    </div>\r\n</div>\r\n<div class=\"navigation-search\">\r\n    <!--  Navigation -->\r\n    <div class=\"navigation-search\">\r\n        <!-- Include Navigation -->\r\n        <nav id=\"navigation\" class=\"main-navigation singlelevelnav\">\r\n            <ul id=\"nav_list\" class=\"top-level-nav\">\r\n                <li class=\"home-link nav-item\">\r\n                    <a href=\"/\"\r\n                        class=\"first-level-link\"><span id=\"nav_home_container\" class=\"ca-gov-icon-home\" aria-hidden=\"true\"></span>Home</a>\r\n                </li>\r\n\r\n                <li class=\"nav-item\">\r\n                    <a asp-page=\"/About\"\r\n                        class=\"first-level-link\"><span class=\"ca-gov-icon-info-bubble\" aria-hidden=\"true\"></span>About</a>\r\n                </li>\r\n\r\n                <li class=\"nav-item\">\r\n                    <a asp-page=\"/Structure\"\r\n                        class=\"first-level-link\"><span class=\"ca-gov-icon-flowchart\" aria-hidden=\"true\"></span>Structure</a>\r\n                </li>\r\n\r\n                <li class=\"nav-item\">\r\n                    <a href=\"https://github.com/Office-of-Digital-Innovation\" target=\"_blank\"\r\n                        class=\"first-level-link\"><span class=\"ca-gov-icon-download\" aria-hidden=\"true\"></span>Download</a>\r\n                </li>\r\n\r\n                <li class=\"nav-item\">\r\n                    <a href=\"https://webstandards.ca.gov\" target=\"_blank\"\r\n                        class=\"first-level-link\"><span class=\"ca-gov-icon-state\" aria-hidden=\"true\"></span>Web\r\n                        Standards</a>\r\n                </li>\r\n\r\n                <li class=\"nav-item\">\r\n                    <a asp-page=\"/Contact\"\r\n                        class=\"first-level-link\"><span class=\"ca-gov-icon-contact-us\" aria-hidden=\"true\"></span>Contact\r\n                        Us</a>\r\n                </li>\r\n\r\n                <li class=\"nav-item\" id=\"nav-item-search\">\r\n                    <button class=\"first-level-link\" aria-label=\"Search Button\"><span class=\"ca-gov-icon-search\" aria-hidden=\"true\"></span>Search</button>\r\n                </li>\r\n\r\n            </ul>\r\n        </nav>\r\n        <div id=\"head-search\" class=\"search-container\">\r\n            <script type=\"text/javascript\">\r\n                var cx = '001779225245372747843:9s-idxui5pk';// Step 7: Update this value with your search engine unique ID. Submit a request to the CDT Service Desk if you don't already know your unique search engine ID.\r\n                        var gcse = document.createElement('script');\r\n                gcse.type = 'text/javascript';\r\n                        gcse.async = true;\r\n                        gcse.src = 'https://cse.google.com/cse.js?cx=' + cx;\r\n                        var s = document.getElementsByTagName('script');\r\n                s[s.length - 1].parentNode.insertBefore(gcse, s[s.length - 1]);\r\n                \r\n            </script>\r\n            <!-- Google Custom Search -->\r\n            <div class=\"container\">\r\n                <form id=\"Search\" class=\"pos-rel\" action=\"/serp.html\">\r\n                    <span class=\"sr-only\" id=\"SearchInput\">Custom Google Search</span>\r\n                    <input type=\"text\" id=\"q\" name=\"q\" aria-labelledby=\"SearchInput\" placeholder=\"Search this website\" class=\"search-textfield height-50 border-0 p-x-sm w-100\" />\r\n                    <button type=\"submit\" class=\"pos-abs gsc-search-button top-0 width-50 height-50 border-0 bg-transparent\"><span class=\"ca-gov-icon-search font-size-30 color-gray\" aria-hidden=\"true\"></span><span class=\"sr-only\">Submit</span></button>\r\n                    <div class=\"width-50 height-50 close-search-btn\">\r\n                        <button class=\"close-search gsc-clear-button width-50 height-50 border-0 bg-transparent pos-rel\" type=\"reset\"><span class=\"sr-only\">Close Search</span><span class=\"ca-gov-icon-close-mark\" aria-hidden=\"true\"></span></button>\r\n                    </div>\r\n                </form>\r\n            </div>\r\n        </div>\r\n    </div>\r\n</div>\r\n<div class=\"header-decoration\"></div>",
                FooterHtmlContent =
                    "<div class=\"container\">\r\n    <div class=\"row\">\r\n        <div class=\"three-quarters\">\r\n            <ul class=\"footer-links\">\r\n                <li><a href=\"#skip-to-content\">Back to Top</a></li>\r\n                <li><a href=\"/use.html\">Conditions of Use</a></li>\r\n                <li><a href=\"/privacy.html\">Privacy Policy</a></li>\r\n                <li><a href=\"/accessibility.html\">Accessibility</a></li>\r\n                <li><a href=\"/contact.html\">Contact Us</a></li>\r\n            </ul>\r\n        </div>\r\n        <div class=\"quarter text-right\">\r\n            <ul class=\"socialsharer-container\">\r\n                <li><a\r\n                        href=\"https://www.flickr.com/groups/californiagovernment\"><span class=\"ca-gov-icon-flickr\" aria-hidden=\"true\"></span><span class=\"sr-only\">Flickr</span></a>\r\n                </li>\r\n                <li><a\r\n                        href=\"https://www.pinterest.com/cagovernment/\"><span class=\"ca-gov-icon-pinterest\" aria-hidden=\"true\"></span><span class=\"sr-only\">Pinterest</span></a>\r\n                </li>\r\n            </ul>\r\n        </div>\r\n    </div>\r\n</div>\r\n<div class=\"copyright\">\r\n    <div class=\"container\">\r\n        Copyright © <script>\r\n            document.write(new Date().getFullYear())\r\n        </script>2020 State of California\r\n    </div>\r\n</div>"
            };
        }
    }
}