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
document.addEventListener("DOMContentLoaded", function(){
    let xhr = new XMLHttpRequest();
    xhr.open('GET', '/Home/GetTOC?page=/Samples', true);
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4 && xhr.status == 200) {
            let data = JSON.parse(xhr.response);
            let ul = document.getElementById("ulListNav");

            data.Items.forEach(function (item) {

                    let title = item.Title.split("/")[1];
                    let anchor = document.createElement('a');
                    anchor.classList.add("lna");
                    anchor.href = "/" + item.UrlPath;
                    anchor.appendChild(document.createTextNode(title));

                    var li = document.createElement('li');
                    li.appendChild(anchor);
                    
                    ul.appendChild(li);
                var t = item.UrlPath;
            });

            var path = window.location.pathname.toLowerCase();
            const links = document.getElementsByClassName("lna");
            for (let i = 0; i < links.length; i++) {
                if (links[i].href.toLowerCase().endsWith(path)) {
                    links[i].classList.add("active");
                }
            }
        }
    }
    xhr.send();
});