/**
 * Gets a Power BI Embed Token.
 * @param {any} workspaceId Workspace or "group" ID.
 * @param {any} reportId Report ID.
 * @returns An embed token object.
 */
async function ccms___GetPowerBiEmbedToken(workspaceId, reportId) {
    const url = "/Home/CCMS_GET_POWER_BI_TOKEN?workspaceId=" + workspaceId + "&reportId=" + reportId;
    const response = await fetch(url, {
        method: "GET"
    });
    return response;
}

/**
 * Launches a Power BI embeded report.
 * @param {any} elementId HTML element (like a DIV) where report will be placed.
 * @param {any} workspaceId Workspace or "group" ID.
 * @param {any} reportId Report ID.
 */
function ccms___LaunchPowerBiReport(elementId, workspaceId, reportId) {

    var reportContainer = document.getElementById(elementId);

    reportContainer.innerHTML = "<div style='height:100%;font-size:2.5em;border: 1px dashed #dadada;display:flex;justify-content:center;align-items:center;'>Loading Power BI report ...</div>";

    var models = window["powerbi-client"].models;

    // Get the embed token object.
    const response = ccms___GetPowerBiEmbedToken(workspaceId, reportId);
    response.then(function (response) {
        const jsonPromise = response.json();
        jsonPromise.then((embedParams) => {
            let reportLoadConfig = {
                type: "report",
                tokenType: models.TokenType.Embed,
                accessToken: embedParams.EmbedToken.Token,
                // You can embed different reports as per your need
                embedUrl: embedParams.EmbedReport[0].EmbedUrl,

                // Enable this setting to remove gray shoulders from embedded report
                // settings: {
                //     background: models.BackgroundType.Transparent
                // }
            };

            // Use the token expiry to regenerate Embed token for seamless end user experience
            // Refer https://aka.ms/RefreshEmbedToken
            tokenExpiry = embedParams.EmbedToken.Expiration;

            // Embed Power BI report when Access token and Embed URL are available
            var report = powerbi.embed(reportContainer, reportLoadConfig);

            // Clear any other loaded handler events
            report.off("loaded");

            // Triggers when a report schema is successfully loaded
            report.on("loaded", function () {
                console.log("Report load successful");
            });

            // Clear any other rendered handler events
            report.off("rendered");

            // Triggers when a report is successfully embedded in UI
            report.on("rendered", function () {
                console.log("Report render successful");
            });

            // Clear any other error handler events
            report.off("error");

            // Handle embed errors
            report.on("error", function (event) {
                var errorMsg = event.detail;

                // Use errorMsg variable to log error in any destination of choice
                console.error(errorMsg);
                return;
            });
        }).catch(function (data) {
            ccms___PowerBiError(data);
        });
    }).catch(function (err) {
        ccms___PowerBiError(err);
    });
}

/**
 * Handles a Power BI error.
 * @param {any} err
 */
function ccms___PowerBiError(err) {
    // Show error container
    var errorContainer = $(".error-container");
    $(".embed-container").hide();
    errorContainer.show();
    
    // Format error message
    var errMessageHtml = "<strong> Error Details: </strong> <br/>" + err.responseText;
    errMessageHtml = errMessageHtml.split("\n").join("<br/>");

    // Show error message on UI
    errorContainer.append(errMessageHtml);
}


