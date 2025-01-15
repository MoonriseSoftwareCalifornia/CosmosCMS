// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/** @private */
export function configureFetch() {
    return false;
}
/** @private */
export function configureAbortController() {
    return false;
}
/** @private */
export function getWS() {
    throw new Error("Trying to import 'ws' in the browser.");
}
/** @private */
export function getEventSource() {
    throw new Error("Trying to import 'eventsource' in the browser.");
}
//# sourceMappingURL=DynamicImports.browser.js.map