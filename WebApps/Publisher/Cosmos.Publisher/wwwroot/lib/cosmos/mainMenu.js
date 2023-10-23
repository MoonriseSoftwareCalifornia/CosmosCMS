document.addEventListener("DOMContentLoaded", function(){
    let xhr = new XMLHttpRequest();
    xhr.open('GET', '/Home/GetTOC?page=/', true);
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4 && xhr.status == 200) {

            let data = JSON.parse(xhr.response);

            let ul = document.getElementById("nav_list");

            data.Items.forEach(function (item) {

                    let title = item.Title.split("/")[1];
                    let anchor = document.createElement('a');
                    anchor.classList.add("first-level-link");
                    anchor.classList.add("main-nav-anchor");
                    anchor.href = "/" + item.UrlPath;
                    anchor.appendChild(document.createTextNode(title));

                    var li = document.createElement('li');
                    li.classList.add("nav-item");
                    li.appendChild(anchor);
                    ul.appendChild(li);

            });

            var path = window.location.pathname.toLowerCase();
            const links = document.getElementsByClassName("main-nav-anchor");
            
            for (let i = 0; i < links.length; i++) {
                if (links[i].href.toLowerCase().endsWith(path)) {
                    links[i].classList.add("active");
                }
            }
        }
    }
    xhr.send();
});