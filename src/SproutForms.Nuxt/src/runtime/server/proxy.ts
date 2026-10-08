import { createError, defineEventHandler, getRequestURL, proxyRequest } from 'h3';
import { useRuntimeConfig } from '#imports';

// The headless API's endpoints, and nothing else on the Umbraco site
const allowedPaths = [
    /^\/definitions\/[^/]+$/,
    /^\/entries\/[0-9a-f-]+$/i,
    /^\/entries\/[0-9a-f-]+\/pages\/\d+\/validate$/i
];

/**
 * Forwards the browser's requests to the headless API with the API key, which stays on this server.
 */
export default defineEventHandler(event => {
    const config = useRuntimeConfig(event).sproutForms;
    const url = getRequestURL(event);
    const path = url.pathname.slice(config.proxyPath.length);

    if (path.includes('..') || path.includes('%') || !allowedPaths.some(pattern => pattern.test(path))) {
        throw createError({ statusCode: 404 });
    }

    const target = config.baseUrl.replace(/\/+$/, '') + config.apiPath + path + url.search;
    return proxyRequest(event, target, {
        headers: config.apiKey ? { 'Api-Key': config.apiKey } : {}
    });
});
