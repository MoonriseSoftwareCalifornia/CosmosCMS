document.getElementById('frmSearch').addEventListener('submit', );

document.getElementById('btnReset').addEventListener('click', function (event) {
    event.preventDefault(); // Prevent the default form submission
    document.getElementById('divResults').style['display'] = 'none';
    document.getElementById('searchTxt').value = '';
    document.getElementById('btnReset').style['display'] = 'none';
})

function handeSearch(event) {
    event.preventDefault(); // Prevent the default form submission
    postData()
        .then(data => {
            document.getElementById('btnReset').style['display'] = 'block';
            let el = document.getElementById('divResults');
            el.style["display"] = "block";

            if (data === null || data.length < 1) {
                el.innerHTML = "<p>No results returned.</p>";
            } else {
                el.innerHTML = ''; // Reset contents.
                const p = document.createElement('p');
                if (data.length === 1) {
                    p.innerText = "Found " + data.length + " result:";
                } else {
                    p.innerText = "Found " + data.length + " results:";
                }

                p.classList.add("underline");
                p.classList.add("underline-offset-2");
                p.classList.add("mb-4");

                el.appendChild(p);

                let links = [];
                data.forEach((item, index) => {
                    links.push({ url: item.urlPath, text: item.title, updated: item.updated, author: item.authorInfo });
                });

                el.appendChild(buildOrderedList(links));
            }
        })
        .catch(error => {
            console.error('Error:', error);
        });

    // Optionally, submit the form data via AJAX or other methods
}

async function postSearchData() {
    // Create a new FormData object
    const formData = new FormData(document.getElementById('frmSearch'));

    // Use the fetch API to make the POST request
    const response = await fetch('/Home/CCMS___SEARCH', {
        method: 'POST',
        body: formData
    });

    // Check if the response is OK (status code 200-299)
    if (!response.ok) {
        throw new Error('Network response was not ok');
    }

    // Parse the JSON response
    return response.json();
}

function buildOrderedList(links) {
    // Create the ordered list element
    const ol = document.createElement('ol');
    ol.style['list-style-type'] = 'decimal';
    ol.classList.add("ms-4");

    const icon = document.createElement('i');
    icon.className = 'ms-1 fa-solid fa-arrow-up-right-from-square'; // Font

    // Iterate over the links array
    links.forEach(link => {
        // Create a list item element
        const li = document.createElement('li');

        // Create an anchor element
        const a = document.createElement('a');
        a.href = link.url;
        a.textContent = link.text;

        const info = document.createElement('span');
        const dt = new Date(link.updated);

        info.style['font-size'] = '0.75rem';
        //info.style['font-style'] = 'italic';
        info.innerText = " (updated: " + dt.toLocaleDateString("en-US") + " )";

        // Append the anchor to the list item
        li.appendChild(a);
        //li.appendChild(document.createElement('br'));
        li.appendChild(info);
        li.appendChild(icon);

        // Append the list item to the ordered list
        ol.appendChild(li);
    });

    // Return the ordered list element
    return ol;
}