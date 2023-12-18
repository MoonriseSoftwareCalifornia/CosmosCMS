"use strict";
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
Object.defineProperty(exports, "__esModule", { value: true });
exports.FetchHttpClient = void 0;
const Errors_1 = require("./Errors");
const HttpClient_1 = require("./HttpClient");
const ILogger_1 = require("./ILogger");
const Utils_1 = require("./Utils");
const DynamicImports_1 = require("./DynamicImports");
class FetchHttpClient extends HttpClient_1.HttpClient {
    constructor(logger) {
        super();
        this._logger = logger;
        // This is how you do "reference" arguments
        const fetchObj = { _fetchType: undefined, _jar: undefined };
        if ((0, DynamicImports_1.configureFetch)(fetchObj)) {
            this._fetchType = fetchObj._fetchType;
            this._jar = fetchObj._jar;
        }
        else {
            this._fetchType = fetch.bind((0, Utils_1.getGlobalThis)());
        }
        this._abortControllerType = AbortController;
        const abortObj = { _abortControllerType: this._abortControllerType };
        if ((0, DynamicImports_1.configureAbortController)(abortObj)) {
            this._abortControllerType = abortObj._abortControllerType;
        }
    }
    /** @inheritDoc */
    async send(request) {
        // Check that abort was not signaled before calling send
        if (request.abortSignal && request.abortSignal.aborted) {
            throw new Errors_1.AbortError();
        }
        if (!request.method) {
            throw new Error("No method defined.");
        }
        if (!request.url) {
            throw new Error("No url defined.");
        }
        const abortController = new this._abortControllerType();
        let error;
        // Hook our abortSignal into the abort controller
        if (request.abortSignal) {
            request.abortSignal.onabort = () => {
                abortController.abort();
                error = new Errors_1.AbortError();
            };
        }
        // If a timeout has been passed in, setup a timeout to call abort
        // Type needs to be any to fit window.setTimeout and NodeJS.setTimeout
        let timeoutId = null;
        if (request.timeout) {
            const msTimeout = request.timeout;
            timeoutId = setTimeout(() => {
                abortController.abort();
                this._logger.log(ILogger_1.LogLevel.Warning, `Timeout from HTTP request.`);
                error = new Errors_1.TimeoutError();
            }, msTimeout);
        }
        if (request.content === "") {
            request.content = undefined;
        }
        if (request.content) {
            // Explicitly setting the Content-Type header for React Native on Android platform.
            request.headers = request.headers || {};
            if ((0, Utils_1.isArrayBuffer)(request.content)) {
                request.headers["Content-Type"] = "application/octet-stream";
            }
            else {
                request.headers["Content-Type"] = "text/plain;charset=UTF-8";
            }
        }
        let response;
        try {
            response = await this._fetchType(request.url, {
                body: request.content,
                cache: "no-cache",
                credentials: request.withCredentials === true ? "include" : "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    ...request.headers,
                },
                method: request.method,
                mode: "cors",
                redirect: "follow",
                signal: abortController.signal,
            });
        }
        catch (e) {
            if (error) {
                throw error;
            }
            this._logger.log(ILogger_1.LogLevel.Warning, `Error from HTTP request. ${e}.`);
            throw e;
        }
        finally {
            if (timeoutId) {
                clearTimeout(timeoutId);
            }
            if (request.abortSignal) {
                request.abortSignal.onabort = null;
            }
        }
        if (!response.ok) {
            const errorMessage = await deserializeContent(response, "text");
            throw new Errors_1.HttpError(errorMessage || response.statusText, response.status);
        }
        const content = deserializeContent(response, request.responseType);
        const payload = await content;
        return new HttpClient_1.HttpResponse(response.status, response.statusText, payload);
    }
    getCookieString(url) {
        let cookies = "";
        if (Utils_1.Platform.isNode && this._jar) {
            // @ts-ignore: unused variable
            this._jar.getCookies(url, (e, c) => cookies = c.join("; "));
        }
        return cookies;
    }
}
exports.FetchHttpClient = FetchHttpClient;
function deserializeContent(response, responseType) {
    let content;
    switch (responseType) {
        case "arraybuffer":
            content = response.arrayBuffer();
            break;
        case "text":
            content = response.text();
            break;
        case "blob":
        case "document":
        case "json":
            throw new Error(`${responseType} is not supported.`);
        default:
            content = response.text();
            break;
    }
    return content;
}
//# sourceMappingURL=FetchHttpClient.js.map