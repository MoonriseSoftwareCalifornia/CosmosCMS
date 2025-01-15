"use strict";
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
Object.defineProperty(exports, "__esModule", { value: true });
exports.getEventSource = exports.getWS = exports.configureAbortController = exports.configureFetch = void 0;
/** @private */
function configureFetch() {
    return false;
}
exports.configureFetch = configureFetch;
/** @private */
function configureAbortController() {
    return false;
}
exports.configureAbortController = configureAbortController;
/** @private */
function getWS() {
    throw new Error("Trying to import 'ws' in the browser.");
}
exports.getWS = getWS;
/** @private */
function getEventSource() {
    throw new Error("Trying to import 'eventsource' in the browser.");
}
exports.getEventSource = getEventSource;
//# sourceMappingURL=DynamicImports.browser.js.map