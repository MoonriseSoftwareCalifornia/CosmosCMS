/**
 * Posts a form and adds the Cosmos antiforgery validation token.
 * @param {any} endpoint
 * @param {any} formName
 * @returns
 */
async function ccms___PostForm(endpoint, formName) {
    const form = document.forms[formName];
    const token = document.head.querySelector("meta[name='cwps-meta-af-value']").content;

    const response = await fetch(endpoint, {
        method: "POST",
        headers: {
            RequestVerificationToken: token
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