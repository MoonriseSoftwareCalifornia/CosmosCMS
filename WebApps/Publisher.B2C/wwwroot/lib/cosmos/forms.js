/**
 * Determines if an alement exists on the current page.
 * @param {any} element
 * @returns {boolean}
 */
function ccms___ElementExists(element) {
    if (typeof (element) === "undefined" || element === null || element === "") {
        return false;
    }
    return true;
}

/**
 * Retrieves the antiforgery token from the server.
 * @returns {string} promise.
 */
async function ccms___GetXsrfToken() {
    return fetch("/ccms__antiforgery/token", {
        method: "GET"
    }).then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok ' + response.statusText);
        }
        const xsrfToken = response.headers.get('XSRF-TOKEN');
        return xsrfToken;
    });
}

/**
 * Posts a form and adds the Cosmos antiforgery validation token.
 * @param {any} endpoint
 * @param {any} formName
 * @returns {Promise<Response>}
 */
async function ccms___PostForm(endpoint, formName) {

    const form = document.forms[formName];

    const xsrfToken = await ccms___GetXsrfToken();

    return fetch(endpoint, {
        method: "POST",
        headers: {
            RequestVerificationToken: xsrfToken
        },
        body: new FormData(form)
    }).then(data => {
        return data;
    });
}