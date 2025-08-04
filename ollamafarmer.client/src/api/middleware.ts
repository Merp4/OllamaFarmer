import type { Middleware } from "openapi-fetch";


export const throwErrorMiddleware: Middleware = {
    async onResponse({ response }) {
        if (!response.ok) {
            // Will produce error messages like "https://example.org/api/v1/example: 404 Not Found".
            throw new Error(`${response.url}: ${response.status} ${response.statusText}`)
        }
    },
};
