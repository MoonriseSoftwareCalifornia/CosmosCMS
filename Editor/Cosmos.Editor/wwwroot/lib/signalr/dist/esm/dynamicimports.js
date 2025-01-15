// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { Platform } from "./Utils";
/** @private */
export function configureFetch(obj) {
    // Node added a fetch implementation to the global scope starting in v18.
    // We need to add a cookie jar in node to be able to share cookies with WebSocket
    if (typeof fetch === "undefined" || Platform.isNode) {
        // Cookies aren't automatically handled in Node so we need to add a CookieJar to preserve cookies across requests
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        obj._jar = new (require("tough-cookie")).CookieJar();
        if (typeof fetch === "undefined") {
            // eslint-disable-next-line @typescript-eslint/no-var-requires
            obj._fetchType = require("node-fetch");
        }
        else {
            // Use fetch from Node if available
            obj._fetchType = fetch;
        }
        // node-fetch doesn't have a nice API for getting and setting cookies
        // fetch-cookie will wrap a fetch implementation with a default CookieJar or a provided one
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        obj._fetchType = require("fetch-cookie")(obj._fetchType, obj._jar);
        return true;
    }
    return false;
}
/** @private */
export function configureAbortController(obj) {
    if (typeof AbortController === "undefined") {
        // Node needs EventListener methods on AbortController which our custom polyfill doesn't provide
        obj._abortControllerType = require("abort-controller");
        return true;
    }
    return false;
}
/** @private */
export function getWS() {
    return require("ws");
}
/** @private */
export function getEventSource() {
    return require("eventsource");
}
//# sourceMappingURL=DynamicImports.js.map