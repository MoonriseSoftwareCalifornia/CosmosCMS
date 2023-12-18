async function ccms___PostForm(endpoint, formName) {
    ccms___AddAftToForm(formName);
    const form = document.forms[formName];
    const response = await fetch(endpoint, {
        method: "POST",
        body: new FormData(form)
    });
    return response;
}

function ccms___AddAftToForm(formName) {
    const form = document.forms[formName];
    const ccmsXrefToken = document.getElementsByName("__RequestVerificationToken")[0].value;
    if (ccms___ElementExists(form)) {
        let el = form.elements["__RequestVerificationToken"];
        if (!ccms___ElementExists(el)) {
            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = "__RequestVerificationToken"; // 'the key/name of the attribute/field that is sent to the server
            input.value = ccmsXrefToken;
            form.appendChild(input);
        } else {
            el.value = ccmsXrefToken;
        }
    }
}

function ccms___ElementExists(element) {
    if (typeof (element) === "undefined" || element === null || element === "") {
        return false;
    }
    return true;
}