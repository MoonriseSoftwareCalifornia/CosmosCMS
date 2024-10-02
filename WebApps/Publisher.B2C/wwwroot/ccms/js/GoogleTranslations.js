$.urlParam = function(name) {
    var results = new RegExp("[?&]" + name + "=([^&#]*)").exec(window.location.href);
    if (results == null) {
        return null;
    } else {
        return decodeURI(results[1]) || 0;
    }
};

$(function() {

    // Get parameters send to this script
    var scriptTag = $("#ccms-lang-script");
    var ccmsLangDisplayName = scriptTag.attr("data-lang");
    // Construct a bootstrap drop down control here.
    var ctrl =
        "<div class=\"input-group mb-2\"><div class=\"dropdown\"><div class=\"input-group-prepend\"><div class=\"input-group-text input-group-text-sm\">Language: </div>";
    ctrl +=
        "<button class=\"form-control btn btn-primary dropdown-toggle\" type=\"button\" id=\"ccms-lang-choice-btn\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\">" +
        ccmsLangDisplayName +
        "</button>";
    ctrl +=
        "<ul id=\"ccms-lang-choice-ddl\" class=\"dropdown-menu dropdown-menu-dark dropdown-menu-right\" aria-labelledby=\"ccms-lang-choice-btn\" style=\"height: auto;max-height: 200px;overflow-x:hidden;\">";
    ctrl += "Loading...";
    ctrl += "</ul></div></div>";

    // The div to hold the language drop down.
    $("#ccms-lang-dd-ctrl").html(ctrl);

    var langCode = $.urlParam("lang");

    if (langCode === null) {
        langCode = "en-US";
    }

    function loadLangList() {
        $.get("/Home/GetSupportedLanguages?lang=" + langCode,
            function(data) {
                var ddList = ""; // Initialize a string object
                $.each(data,
                    function(index, item) {
                        ddList += "<li><a class=\"dropdown-item\" href=\"javascript:langSelect('" +
                            item.LanguageCode +
                            "', '" +
                            item.DisplayName +
                            "')\">" +
                            item.DisplayName +
                            "</a></li>";
                        if (item.languageCode === langCode) {
                            $("#ccms-lang-choice-btn").html(item.DisplayName);
                        }
                    });
                $("#ccms-lang-choice-ddl").html(ddList);
            });
    }

    // modify URLs for language choice
    if (langCode.toLowerCase().startsWith("en") === false) {
        var elements = $("a");
        if (elements !== null && typeof (elements) !== "undefined" && elements.length > 0) {
            var prefixes = getPrefixes();
            $.each(elements,
                function(index, item) {
                    var href = $(item).attr("href");
                    if (href !== null && typeof (href) !== "undefined") {
                        href = href.toLowerCase();
                        $.each(prefixes,
                            function(i, pre) {
                                if (href.startsWith(pre) && href.endsWith(langCode.toLowerCase()) === false) {
                                    href = href.split("?")[0] + "?lang=" + langCode;
                                    $(item).attr("href", href);
                                }
                            });
                    }
                });
        }
    }

    var toast =
        "<div id=\"cms-lang-choice-toast\" class=\"toast\" role=\"alert\" aria-live=\"assertive\" aria-atomic=\"true\" data-delay=\"3000\" style=\"position: absolute; min-height: 200px; top: 200px; right: 100px;\">";
    toast += "<div class=\"toast-header\">";
    toast += "<strong class=\"mr-auto\">Language Change</strong>";
    toast += "<small>Working...</small>";
    toast += "<button type=\"button\" class=\"ml-2 mb-1 close\" data-dismiss=\"toast\" aria-label=\"Close\">";
    toast += "<span aria-hidden=\"true\">&times;</span>";
    toast += "</button>";
    toast += "</div>";
    toast += "<div class=\"toast-body\">Please wait while your language choice takes affect...";
    toast += "</div>";

    $("body").append(toast);

    if (typeof langDropDownCtrl != "undefined") {
        $(langDropDownCtrl).on("show.bs.dropdown",
            function () {
                // do something…
                loadLangList();
            });
    }
});

function getPrefixes() {
    var dns = window.location.hostname.toLowerCase();
    var prefixes = new Array("/", "https://" + dns, "http://" + dns, "https://" + dns + "/", "http://" + dns + "/");
    if (!dns.startsWith("www.")) {
        var www = "www." + dns;
        prefixes.push("https://" + www);
        prefixes.push("https://" + www);
        prefixes.push("http://" + www + "/");
        prefixes.push("https://" + www + "/");
    }
    return prefixes;
}

function langSelect(lang, lbl) {
    $("#ccms-lang-choice-btn").html(lbl);
    var t = $("#cms-lang-choice-toast");
    t.toast("show");
    if (lang.startsWith("en")) {
        window.location.href = window.location.href.split("?")[0];
    } else {
        window.location.href = window.location.href.split("?")[0] + "?lang=" + lang;
    }
    // Re-get this page with the new language cookie set.
}