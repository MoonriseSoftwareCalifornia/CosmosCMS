/**
 * Posts a form and adds the Cosmos antiforgery validation token.
 * @param {any} endpoint
 * @param {any} formName
 * @returns
 */
async function ccms___PostForm(endpoint, formName) {

    const form = document.forms[formName];

    let result = await fetch("/CCMS-XSRF-TOKEN", {
        method: "GET"
    });

    const xsrfToken = result.cookie
        .split("; ")
        .find(row => row.startsWith("XSRF-TOKEN="))
        .split("=")[1];

    const response = await fetch(endpoint, {
        method: "POST",
        headers: {
            RequestVerificationToken: xsrfToken
        },
        body: new FormData(form)
    });
    return response;
}
/**
 * Determines if an alement exists on the current page.
 * @param {any} element
 * @returns
 */
function ccms___ElementExists(element) {
    if (typeof (element) === "undefined" || element === null || element === "") {
        return false;
    }
    return true;
}

