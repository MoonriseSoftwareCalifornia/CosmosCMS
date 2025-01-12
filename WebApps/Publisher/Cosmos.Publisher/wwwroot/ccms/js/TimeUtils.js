//
// Utilities related to formating time.
//

// Get the time zone of the user's computer
function getLocalTimeZone() {
    var datetime = new Date();
    var dateTimeString = datetime.toString();
    var timezone = dateTimeString.substring(dateTimeString.indexOf("(") - 1);
    return timezone;
}

function getLocalTime(gmt) {

    var dateTime = new Date(gmt);
    var test = dateTime.toString();

    if (test === "Invalid Date") {
        return gmt;
    }

    var d = dateTime.toLocaleDateString();
    var t = dateTime.toLocaleTimeString();
    var dateTimeString = dateTime.toString();
    var z = dateTimeString.substring(dateTimeString.indexOf("(") - 1);

    return "<span title='" + dateTime.toUTCString() + "'>" +  d + " " + t + " " + z + "</span>";
}