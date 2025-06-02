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
 * Posts a form and adds the Cosmos antiforgery validation token.
 * @param {any} endpoint
 * @param {any} formName
 * @returns {Promise<Response>}
 */
async function ccms___PostForm(endpoint, formName) {

    const form = document.forms[formName];
    const value = `; ${document.cookie}`;
    const name = "X-XSRF-TOKEN";
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
        const xsrfToken = parts.pop().split(';').shift();
        return fetch(endpoint, {
            method: "POST",
            headers: {
                RequestVerificationToken: xsrfToken
            },
            body: new FormData(form)
        }).then(data => {
            return data;
        });
    } else {
        throw new Error('X-XSRF-TOKEN cookie not found');
    }
}