import type { FormClientModel, HeadlessOutcome, HeadlessSubmitResponse, ValidationProblemDetails } from './api/types.gen';
import { getSubmissionGuardValues } from './guards';
import type { FormErrors, FormValues } from './types';

const apiPath = '/umbraco/sproutforms/delivery/api/v1';

export interface SproutFormsClientOptions {
    // The Umbraco site, such as https://cms.example.com
    baseUrl: string;
    // Only when SproutForms:Headless:ApiKey is set, and only from a server: a key in browser code is public
    apiKey?: string;
    fetch?: typeof fetch;
    headers?: Record<string, string>;
}

export interface SubmitOptions {
    // Keyed by field alias; a File or Blob value is sent as an upload
    values: FormValues;
    // Where the form was filled in; defaults to the current page in a browser. Only stored when it is on an allowed origin
    pageUrl?: string;
    // Added to what the form's submission guard handler returns
    guard?: Record<string, string>;
    signal?: AbortSignal;
}

export type SubmitResult =
    | { ok: true; outcome: HeadlessOutcome | null }
    | { ok: false; errors: FormErrors };

/**
 * A request the API didn't answer as expected: a form that doesn't exist (404), a missing API key (401) or a server error.
 */
export class SproutFormsApiError extends Error {
    constructor(public readonly status: number, message: string) {
        super(message);
        this.name = 'SproutFormsApiError';
    }
}

function isFile(value: unknown): value is Blob {
    return typeof Blob !== 'undefined' && value instanceof Blob;
}

// The ETag of a definition is its version
function toETag(versionId: string): string {
    return `"${versionId.replace(/-/g, '')}"`;
}

export function createSproutFormsClient(options: SproutFormsClientOptions) {
    const fetchImpl = options.fetch ?? globalThis.fetch.bind(globalThis);
    const baseUrl = options.baseUrl.replace(/\/+$/, '') + apiPath;

    function headers(extra: Record<string, string> = {}): Record<string, string> {
        return {
            ...options.headers,
            ...(options.apiKey ? { 'Api-Key': options.apiKey } : {}),
            ...extra
        };
    }

    async function fail(response: Response): Promise<never> {
        throw new SproutFormsApiError(response.status, `SproutForms answered ${response.status} ${response.statusText}`.trim());
    }

    async function readErrors(response: Response): Promise<FormErrors> {
        const problem = await response.json() as ValidationProblemDetails;
        return problem.errors ?? {};
    }

    async function postJson(url: string, body: unknown, signal?: AbortSignal): Promise<Response> {
        return fetchImpl(url, {
            method: 'POST',
            headers: headers({ 'Content-Type': 'application/json' }),
            body: JSON.stringify(body),
            signal
        });
    }

    return {
        /**
         * The published version of a form, by id or alias.
         */
        async getDefinition(idOrAlias: string, init: { signal?: AbortSignal } = {}): Promise<FormClientModel> {
            const response = await fetchImpl(`${baseUrl}/definitions/${encodeURIComponent(idOrAlias)}`, { headers: headers(), signal: init.signal });
            if (!response.ok) return fail(response);
            return await response.json() as FormClientModel;
        },

        /**
         * The form's published version when it changed since the one you have, otherwise null.
         */
        async revalidateDefinition(definition: FormClientModel, init: { signal?: AbortSignal } = {}): Promise<FormClientModel | null> {
            const response = await fetchImpl(`${baseUrl}/definitions/${definition.id}`, {
                headers: headers({ 'If-None-Match': toETag(definition.versionId) }),
                signal: init.signal
            });
            if (response.status === 304) return null;
            if (!response.ok) return fail(response);
            return await response.json() as FormClientModel;
        },

        /**
         * Checks the rules of one page only the server knows, before the visitor moves on. Nothing is saved, and uploads aren't
         * sent. Returns the errors; none means the page is valid.
         */
        async validatePage(definition: FormClientModel, pageIndex: number, values: FormValues, init: { signal?: AbortSignal } = {}): Promise<FormErrors> {
            const textValues = Object.fromEntries(Object.entries(values).filter(([, value]) => !isFile(value)));
            const response = await postJson(`${baseUrl}/entries/${definition.id}/pages/${pageIndex}/validate`, { values: textValues }, init.signal);
            if (response.status === 400) return readErrors(response);
            if (!response.ok) return fail(response);
            return {};
        },

        /**
         * Submits the form, with the values its submission guard needs. Sent as multipart/form-data when a value is a file.
         */
        async submit(definition: FormClientModel, submit: SubmitOptions): Promise<SubmitResult> {
            const files = Object.entries(submit.values).filter((entry): entry is [string, Blob] => isFile(entry[1]));
            const values = Object.fromEntries(Object.entries(submit.values).filter(([, value]) => !isFile(value)));
            const guard = { ...await getSubmissionGuardValues(definition, submit.values), ...submit.guard };
            const pageUrl = submit.pageUrl ?? (globalThis as { location?: Location }).location?.href;
            const url = `${baseUrl}/entries/${definition.id}`;

            let response: Response;
            if (files.length === 0) {
                response = await postJson(url, { values, pageUrl, guard }, submit.signal);
            } else {
                const body = new FormData();
                body.append('values', JSON.stringify(values));
                body.append('guard', JSON.stringify(guard));
                if (pageUrl) body.append('pageUrl', pageUrl);
                for (const [alias, file] of files) {
                    body.append(alias, file, file instanceof File ? file.name : alias);
                }
                // No Content-Type: the browser sets it, with the multipart boundary
                response = await fetchImpl(url, { method: 'POST', headers: headers(), body, signal: submit.signal });
            }

            if (response.status === 400) return { ok: false, errors: await readErrors(response) };
            if (!response.ok) return fail(response);

            const result = await response.json() as HeadlessSubmitResponse;
            return { ok: true, outcome: result.outcome ?? null };
        }
    };
}

export type SproutFormsClient = ReturnType<typeof createSproutFormsClient>;
