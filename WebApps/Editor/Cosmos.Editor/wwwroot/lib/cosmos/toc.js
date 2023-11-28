/*
    Automatically builds a table of contents (TOC) using the Cosmos CMS API

    Use 'DOMContentLoaded' event to fire after DOM is loaded and before images and CSS
    Use 'load' event to fire after everything is loaded and parsed

    Endpoint:   /Home/GetTOC
    Method:     GET
    Arguments: 
        &page={Parent page URL}
        &pageNo={page number}
        &pageSize={number of records to return}
        &orderByPub={false|true}
        
    Notes:
        * The "page" argument is required, the others are optional.
        * The pageNumber and pageSize are used for pagination.
        * orderByPub defaults to false, which means the results are returned sorted by URL
    
    Examples:
        /Home/GetTOC?page=/Documentation
        /Home/GetTOC?page=/Samples&pageNo=1&pageSize=10&orderByPub=true

    Example return JSON:

    {
        "PageNo": 0,
        "PageSize": 10,
        "TotalCount": 3,
        "Items": [
            {
                "UrlPath": "samples/accordion",
                "Title": "Samples/Accordion",
                "Published": "2023-10-01T15:18:49+00:00",
                "Updated": "2023-10-01T15:24:00.1684843+00:00",
                "BannerImage": null,
                "AuthorInfo": "null"
            },
            {
                "UrlPath": "samples/accordion_list",
                "Title": "Samples/Accordion List",
                "Published": "2023-09-30T18:11:51+00:00",
                "Updated": "2023-09-30T18:11:52.1851412+00:00",
                "BannerImage": null,
                "AuthorInfo": "null"
            },
            {
                "UrlPath": "samples/banner",
                "Title": "Samples/Banner",
                "Published": "2023-09-30T17:41:48+00:00",
                "Updated": "2023-09-30T18:05:57.5869856+00:00",
                "BannerImage": null,
                "AuthorInfo": "null"
            }
        ]
    }
*/
//
// EXAMPLE IMPLEMENTATION BELOW
//
/**
 * Automatically builds a table of contents using the page path set
 * in the HEAD tag: meta[name='cwps-meta-path-url'].
 * This executes a "DOMContentLoaded" event listener.  It retrieves all the pages one level below THIS web page.
 * @param {string} navElementId
 * @param {string} anchorClassName
 * @param {string} activeCssClass
 */
function cosmos_cms_build_toc_default(navElementId, anchorClassName, activeCssClass) {
    // Get the page path from meta tag.
    let pagePath = document.querySelector("meta[name='cwps-meta-path-url']").getAttribute("content");
    if (pagePath === "root") {
        pagePath = "";
    }
    cosmos_cms_build_toc(pagePath, navElementId, anchorClassName, activeCssClass);
}

/**
 * Builds a table of contents using the Cosmos CMS Table of Contents API (end point "/Home/GetTOC?page=/{pathtorootpage}")
 * This executes a "DOMContentLoaded" event listener.  It retrieves all the pages one level below the given {pathtorootpage}.
 * @param {string} pagePath Page path (without leading slash '/')
 * @param {string} navElementId ID of page <UL> or <OL> element that the <LI> elements will be inserted into.
 * @param {string} anchorClassName Anchor <A> class name (optional).
 * @param {string} activeCssClass The CSS class for an "active" or "selected" element (optional).
 */
function cosmos_cms_build_toc(pagePath, navElementId, liClassName, anchorClassName, activeCssClass) {

    let ul = document.getElementById(navElementId);

    if (typeof (ul) === "undefined" || ul === null) {
        console.error("ERROR: cosmos_cms_build_toc() - Could not find TOC element: " + navElementId);
        return;
    }

    document.addEventListener("DOMContentLoaded", function () {

        const cosmosAnchorItemClass = "cosmos_Tock_Anchor_Item";

        let xhr = new XMLHttpRequest();
        xhr.open('GET', '/Home/GetTOC?page=/' + pagePath, true);

        xhr.onreadystatechange = function () {
            if (xhr.readyState == 4 && xhr.status == 200) {

                let data = JSON.parse(xhr.response);
                
                data.Items.forEach(function (item) {

                    let title = item.Title.split("/").at(-1);
                    let anchor = document.createElement('a');
                    let li = document.createElement('li');

                    if (typeof (liClassName) !== "undefined" && liClassName !== null && liClassName !== "") {
                        li.className = liClassName;
                    }

                    anchor.classList.add(cosmosAnchorItemClass);

                    if (typeof (anchorClassName) !== "undefined" && anchorClassName !== null && anchorClassName !== "") {
                        anchor.className = anchorClassName;
                    }

                    anchor.href = "/" + item.UrlPath;
                    anchor.appendChild(document.createTextNode(title));

                    li.appendChild(anchor);

                    ul.appendChild(li);
                });

                // If an active class is set, apply it here
                if (typeof (activeCssClass) !== "undefined" && activeCssClass !== null && activeCssClass !== "") {
                    var path = window.location.pathname.toLowerCase();
                    const links = document.getElementsByClassName(cosmosAnchorItemClass);
                    for (let i = 0; i < links.length; i++) {
                        if (links[i].href.toLowerCase().endsWith(path)) {
                            links[i].classList.add(activeCssClass);
                        }
                    }
                }
            }
        }
        xhr.send();
    });
}

/**
 * Builds "bread crumb" navigation using the current page path found
 * in the PAGE HEAD META tag meta[name='cwps-meta-path-url'].
 * @param {string} breadCrumbId ID of UL or OL element within which to build the bread crumbs (<li> elements with <a> anchors).
 * @returns
 */
function cosmos_cms_build_breadcrumbs(breadCrumbId) {

    let breadCrumbs = document.getElementById(breadCrumbId);

    if (typeof (breadCrumbs) === "undefined" || breadCrumbs === null) {
        console.error("ERROR: cosmos_cms_build_breadcrumbs() - Could not find breadcrumb element: " + breadCrumbId);
        return;
    }

    document.addEventListener("DOMContentLoaded", function () {

        const pathArray = document.head.querySelector("meta[name='cwps-meta-path-url']").content.split("/");
        let path = "";
        const titleArray = document.title.split("/");

        for (let i = 0; i < titleArray.length; i++) {
            let title = titleArray[i];
            path = path + "/" + pathArray[i];
            let anchor = document.createElement('a');
            anchor.appendChild(document.createTextNode(title));
            anchor.href = path;
            var li = document.createElement('li');
            li.appendChild(anchor);
            breadCrumbs.appendChild(li);
        }

    });
}
