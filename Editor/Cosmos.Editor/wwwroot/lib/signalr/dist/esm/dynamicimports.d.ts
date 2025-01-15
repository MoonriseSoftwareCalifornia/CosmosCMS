import { CookieJar } from "@types/tough-cookie";
/** @private */
export declare function configureFetch(obj: {
    _fetchType?: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    _jar?: CookieJar;
}): boolean;
/** @private */
export declare function configureAbortController(obj: {
    _abortControllerType: {
        prototype: AbortController;
        new (): AbortController;
    };
}): boolean;
/** @private */
export declare function getWS(): any;
/** @private */
export declare function getEventSource(): any;
