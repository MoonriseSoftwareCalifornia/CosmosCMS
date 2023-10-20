// Cosmos CMS utility functions

$(document).ready(function () {
    cosmosBuildTOC(".divCcmsToc");
});

// Automatically creates a Table of Contents for a given page
function cosmosBuildTOC(targetClassId, startTitle, ordByPubDate, pageNo, pageSize,) {

    if (startTitle === null || typeof (startTitle) === "undefined" || startTitle === "") {
        startTitle = document.title;
    }
    if (ordByPubDate === null || typeof (ordByPubDate) === "undefined") {
        ordByPubDate = false;
    } else {
        ordByPubDate = Boolean(ordByPubDate);
    }

    cosmosGetTOC(startTitle, ordByPubDate, pageNo, pageSize, function (data) {
        var html = "<ul>";
        data.Items.forEach(function (item) {
            html += "<li><a href='/" + item.UrlPath + "'>" + item.Title.substring(item.Title.lastIndexOf("/") + 1) + "</a></li>";
        });
        html += "</ul>";

        html += "<div>";

        if (data.PageNo > 0) {
            html += "<span onclick=''>Prev</span>";
        }
        var current = (data.PageNo * data.PageSize) + data.PageSize;

        if (current < data.TotalCount) {
            html += "<span onclick='' style='float:right'>Next</span>";
        }

        html += "</div>";
        $(targetClassId).html(html);
    });
}

// Cosmos CMS Table of Contents generator
function cosmosGetTOC(parentTitle, orderbypub, pageNo, pageSize, callback) {

    if (orderbypub === null || typeof (orderbypub) === "undefined") {
        orderbypub = false;
    }

    if (pageNo === null || typeof (pageNo) === "undefined") {
        pageNo = 0;
    }

    if (pageSize === null || typeof (pageSize) === "undefined") {
        pageSize = 10;
    }

    $.ajax({
        type: "GET",
        url: "/Home/GetTOC",
        data: { page: encodeURIComponent(parentTitle), orderByPub: orderbypub, pageNo: pageNo, pageSize: pageSize },
        success: callback,
        dataType: "json"
    });

}
