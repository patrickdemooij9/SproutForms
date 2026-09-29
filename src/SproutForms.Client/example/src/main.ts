import { createSproutFormsClient, getFields, isFieldGroup, loadSubmissionGuard, SproutFormsApiError, type FormClientModel, type SproutFormsClient } from '@sproutforms/client';
import { element, mountForm } from './form-renderer';

interface LoggedRequest {
    method: string;
    url: string;
    status: number | 'failed';
    milliseconds: number;
    requestHeaders: Record<string, string>;
    requestBody: string;
    responseHeaders: Record<string, string>;
    responseBody: string;
}

interface TestForm {
    alias: string;
    name: string;
    source: string;
}

const settingsKey = 'sproutforms-playground';
const defaults = {
    // The demo site's AiTest profile; set VITE_SPROUTFORMS_URL for another Umbraco site with SproutForms:Headless:Enabled
    baseUrl: import.meta.env.VITE_SPROUTFORMS_URL ?? 'http://localhost:5970',
    apiKey: '',
    form: 'aiTestMultiPage',
    skipClientValidation: false,
    fillHoneypot: false
};
const settings = { ...defaults, ...readSettings() };
const params = new URLSearchParams(window.location.search);
if (params.get('form')) settings.form = params.get('form')!;

const requests: LoggedRequest[] = [];
let client: SproutFormsClient;
let definition: FormClientModel | undefined;

const $ = <T extends HTMLElement>(selector: string) => document.querySelector<T>(selector)!;

// Every call the client makes goes through here, so the Requests tab shows exactly what went over the wire
async function loggingFetch(input: RequestInfo | URL, init: RequestInit = {}): Promise<Response> {
    const started = performance.now();
    const entry: LoggedRequest = {
        method: init.method ?? 'GET',
        url: String(input),
        status: 'failed',
        milliseconds: 0,
        requestHeaders: { ...(init.headers as Record<string, string> | undefined) },
        requestBody: describeBody(init.body),
        responseHeaders: {},
        responseBody: ''
    };
    requests.unshift(entry);
    try {
        const response = await fetch(input, init);
        entry.status = response.status;
        response.headers.forEach((value, name) => { entry.responseHeaders[name] = value; });
        entry.responseBody = prettyJson(await response.clone().text());
        return response;
    } catch (error) {
        // A CORS rejection shows up here: the browser blocks the response without a status
        entry.responseBody = `${error}\n\nIs the site running, SproutForms:Headless:Enabled on, and ${window.location.origin} in SproutForms:Headless:AllowedOrigins?`;
        throw error;
    } finally {
        entry.milliseconds = Math.round(performance.now() - started);
        renderRequests();
    }
}

function describeBody(body: BodyInit | null | undefined): string {
    if (!body) return '';
    if (typeof body === 'string') return prettyJson(body);
    if (body instanceof FormData) {
        return [...body.entries()]
            .map(([name, value]) => typeof value === 'string' ? `${name}: ${value}` : `${name}: <file ${value.name}, ${value.size} bytes, ${value.type || 'no type'}>`)
            .join('\n');
    }
    return String(body);
}

function prettyJson(text: string): string {
    try {
        return JSON.stringify(JSON.parse(text), null, 2);
    } catch {
        return text;
    }
}

function readSettings(): Partial<typeof defaults> {
    try {
        return JSON.parse(localStorage.getItem(settingsKey) ?? '{}');
    } catch {
        return {};
    }
}

function saveSettings() {
    try {
        localStorage.setItem(settingsKey, JSON.stringify(settings));
    } catch {
        // Only a convenience
    }
}

async function loadForms() {
    const select = $<HTMLSelectElement>('#form-select');
    try {
        // Through the Vite proxy: the test endpoints only exist in the demo site, and have no CORS
        const forms = await (await fetch('/ai-test/forms')).json() as TestForm[];
        select.replaceChildren(...forms.map(form => element('option', { value: form.alias }, `${form.name} (${form.alias}, ${form.source})`)));
    } catch {
        select.replaceChildren(element('option', { value: settings.form }, settings.form));
    }
    if (![...select.options].some(option => option.value === settings.form)) {
        select.append(element('option', { value: settings.form }, settings.form));
    }
    select.value = settings.form;
}

async function loadForm() {
    saveSettings();
    const url = new URL(window.location.href);
    url.searchParams.set('form', settings.form);
    history.replaceState(null, '', url);

    client = createSproutFormsClient({ baseUrl: settings.baseUrl, apiKey: settings.apiKey || undefined, fetch: loggingFetch });
    definition = undefined;
    renderDefinition();
    const root = $('#form');
    root.replaceChildren(element('p', { class: 'muted' }, 'Loading…'));

    try {
        definition = await client.getDefinition(settings.form);
        await loadSubmissionGuard(definition);
    } catch (error) {
        const reason = error instanceof SproutFormsApiError
            ? `SproutForms answered ${error.status}.${error.status === 404 ? ' Is the form published, and the headless API enabled?' : ''}${error.status === 401 ? ' Set the API key.' : ''}`
            : 'The request failed; see the Requests tab.';
        root.replaceChildren(element('p', { class: 'error' }, `The form could not be loaded. ${reason}`));
        return;
    }

    renderDefinition();
    prefillRawRequest();
    mountForm(root, {
        client,
        definition,
        skipClientValidation: () => settings.skipClientValidation,
        beforeSubmit: values => {
            const guard = definition?.submissionGuard;
            if (settings.fillHoneypot && guard?.alias === 'honeypot') {
                values[String((guard.settings as { fieldName?: string }).fieldName)] = 'I am a bot';
            }
        },
        onSubmitted: result => {
            if (result.ok) void loadSubmission();
            showTab(result.ok ? 'submission' : 'requests');
        }
    });
}

function renderRequests() {
    $('#requests-count').textContent = String(requests.length);
    $('#requests').replaceChildren(...requests.map(entry => {
        const ok = typeof entry.status === 'number' && entry.status < 400;
        const details = element('details', { class: 'request' });
        const summary = element('summary');
        summary.append(
            element('span', { class: `status ${ok ? 'ok' : 'bad'}` }, String(entry.status)),
            element('span', { class: 'method' }, entry.method),
            element('span', { class: 'url' }, entry.url.replace(settings.baseUrl, '')),
            element('span', { class: 'muted' }, `${entry.milliseconds} ms`)
        );
        details.append(summary);
        if (Object.keys(entry.requestHeaders).length) details.append(section('Request headers', formatHeaders(entry.requestHeaders)));
        if (entry.requestBody) details.append(section('Request body', entry.requestBody));
        details.append(section('Response headers', formatHeaders(entry.responseHeaders)));
        details.append(section('Response body', entry.responseBody || '(empty)'));
        return details;
    }));
    $<HTMLDetailsElement>('#requests details')?.setAttribute('open', '');
}

function formatHeaders(headers: Record<string, string>): string {
    return Object.entries(headers).map(([name, value]) => `${name}: ${name.toLowerCase() === 'api-key' ? '•••' : value}`).join('\n') || '(none the browser lets a script read)';
}

function section(title: string, text: string): HTMLElement {
    const wrapper = element('div');
    wrapper.append(element('h4', {}, title), element('pre', {}, text));
    return wrapper;
}

function renderDefinition() {
    $('#definition').textContent = definition ? JSON.stringify(definition, null, 2) : '';
}

async function loadSubmission() {
    const target = $('#submission');
    target.textContent = 'Loading…';
    try {
        const response = await fetch(`/ai-test/forms/${encodeURIComponent(settings.form)}/submissions?take=1`);
        const result = await response.json() as { items: unknown[] };
        target.textContent = result.items.length
            ? JSON.stringify(result.items[0], null, 2)
            : 'No submissions yet.';
    } catch {
        target.textContent = 'The stored submission is read from the demo site\'s /ai-test endpoints, which only exist in its AiTest environment.';
    }
}

function prefillRawRequest() {
    if (!definition) return;
    const values = Object.fromEntries(getFields(definition).filter(field => field.type !== 'file').map(field => [field.alias, isFieldGroup(field) ? [] : '']));
    const guard = definition.submissionGuard?.alias === 'honeypot'
        ? { [String((definition.submissionGuard.settings as { fieldName?: string }).fieldName)]: '' }
        : {};
    $<HTMLTextAreaElement>('#raw-body').value = JSON.stringify({ values, pageUrl: window.location.href, guard }, null, 2);
    $<HTMLInputElement>('#raw-path').value = `entries/${definition.id}`;
}

async function sendRawRequest() {
    const path = $<HTMLInputElement>('#raw-path').value.replace(/^\/+/, '');
    const body = $<HTMLTextAreaElement>('#raw-body').value;
    const headers: Record<string, string> = { 'Content-Type': $<HTMLSelectElement>('#raw-type').value };
    if (settings.apiKey) headers['Api-Key'] = settings.apiKey;
    try {
        const response = await loggingFetch(`${settings.baseUrl}/umbraco/sproutforms/delivery/api/v1/${path}`, {
            method: $<HTMLSelectElement>('#raw-method').value,
            headers,
            body: $<HTMLSelectElement>('#raw-method').value === 'GET' ? undefined : body
        });
        $('#raw-response').textContent = `${response.status} ${response.statusText}\n\n${prettyJson(await response.text())}`;
    } catch (error) {
        $('#raw-response').textContent = String(error);
    }
}

function showTab(name: string) {
    document.querySelectorAll<HTMLElement>('[data-tab]').forEach(tab => tab.setAttribute('aria-selected', String(tab.dataset.tab === name)));
    document.querySelectorAll<HTMLElement>('[data-panel]').forEach(panel => { panel.hidden = panel.dataset.panel !== name; });
    if (name === 'submission') void loadSubmission();
}

function bindSettings() {
    const baseUrl = $<HTMLInputElement>('#base-url');
    const apiKey = $<HTMLInputElement>('#api-key');
    const skip = $<HTMLInputElement>('#skip-validation');
    const honeypot = $<HTMLInputElement>('#fill-honeypot');
    baseUrl.value = settings.baseUrl;
    apiKey.value = settings.apiKey;
    skip.checked = settings.skipClientValidation;
    honeypot.checked = settings.fillHoneypot;

    baseUrl.addEventListener('change', () => { settings.baseUrl = baseUrl.value.replace(/\/+$/, ''); void loadForm(); });
    apiKey.addEventListener('change', () => { settings.apiKey = apiKey.value; void loadForm(); });
    skip.addEventListener('change', () => { settings.skipClientValidation = skip.checked; saveSettings(); });
    honeypot.addEventListener('change', () => { settings.fillHoneypot = honeypot.checked; saveSettings(); });
    $<HTMLSelectElement>('#form-select').addEventListener('change', event => {
        settings.form = (event.target as HTMLSelectElement).value;
        void loadForm();
    });
    $('#reload').addEventListener('click', () => void loadForm());
    $('#clear-requests').addEventListener('click', () => { requests.length = 0; renderRequests(); });
    $('#refresh-submission').addEventListener('click', () => void loadSubmission());
    $('#raw-send').addEventListener('click', () => void sendRawRequest());
    $('#raw-reset').addEventListener('click', prefillRawRequest);
    document.querySelectorAll<HTMLElement>('[data-tab]').forEach(tab => tab.addEventListener('click', () => showTab(tab.dataset.tab!)));
}

bindSettings();
showTab('requests');
void loadForms().then(loadForm);
