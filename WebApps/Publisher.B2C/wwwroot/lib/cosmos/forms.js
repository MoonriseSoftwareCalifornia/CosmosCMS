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
 * @returns {string}
 */
async function ccms___GetXsrfToken() {

    let result = await fetch("/CCMS-XSRF-TOKEN", {
        method: "GET"
    });

    return result.cookie
        .split("; ")
        .find(row => row.startsWith("XSRF-TOKEN="))
        .split("=")[1];
}

/**
 * Posts a form and adds the Cosmos antiforgery validation token.
 * @param {any} endpoint
 * @param {any} formName
 * @returns {Promise<Response>}
 */
async function ccms___PostForm(endpoint, formName) {

    const form = document.forms[formName];

    const xsrfToken = ccms___GetXsrfToken();

    const response = await fetch(endpoint, {
        method: "POST",
        headers: {
            RequestVerificationToken: xsrfToken
        },
        body: new FormData(form)
    });
    return response;
}