/**
 * Builds a Bootstrap style navigation tree of contents using the Cosmos CMS Table of Contents API (end point "/Home/GetTOC?page=/{pathtorootpage}")
 * This executes a "DOMContentLoaded" event listener.  It retrieves all the pages one level below the given {pathtorootpage}.
 * @param {string} pagePath Page path (without leading slash '/')
 * @param {string} navElementId ID of page <UL> or <OL> element that the <LI> elements will be inserted into.
 * @param {string} anchorClassName Anchor <A> class name (optional).
 * @returns {Promise}
 */
async function ccms___NavBuilder(pagePath, navElementId, liClassName, anchorClassName) {

    // This is the UL element to hold the
    let ul = document.getElementById(navElementId);

    if (typeof (ul) === "undefined" || ul === null) {
        console.error("ERROR: cosmos_cms_build_toc() - Could not find TOC element: " + navElementId);
        return;
    }

    const response = await fetch('/Home/GetTOC?page=/' + pagePath, {
        method: "GET"
    });

    const data = await response.json();
    // Process the results here

    data.Items.forEach(function (item) {

        let title = item.Title.split("/").at(-1);
        let anchor = document.createElement('a');
        let li = document.createElement('li');

        if (typeof (liClassName) !== "undefined" && liClassName !== null && liClassName !== "") {
            li.className = liClassName;
        }

        if (typeof (anchorClassName) !== "undefined" && anchorClassName !== null && anchorClassName !== "") {
            anchor.className = anchorClassName;
        }

        anchor.href = "/" + item.UrlPath;
        anchor.appendChild(document.createTextNode(title));

        li.appendChild(anchor);

        ul.appendChild(li);
    });
}
