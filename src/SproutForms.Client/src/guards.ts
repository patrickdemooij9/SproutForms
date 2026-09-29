import type { FormClientModel } from './api/types.gen';
import type { FormValues } from './types';

/**
 * Satisfies a submission guard in the browser. getValues returns what the guard checks, sent as the submission's "guard".
 */
export interface SubmissionGuardHandler {
    load?(settings: Record<string, unknown>): Promise<void>;
    getValues(settings: Record<string, unknown>, values: FormValues): Promise<Record<string, string>>;
}

const guards = new Map<string, SubmissionGuardHandler>();

/**
 * Adds or replaces the handler for a submission guard alias, for a guard you registered on the server.
 */
export function registerSubmissionGuard(alias: string, handler: SubmissionGuardHandler): void {
    guards.set(alias, handler);
}

function settingsOf(definition: FormClientModel): Record<string, unknown> {
    return (definition.submissionGuard?.settings ?? {}) as Record<string, unknown>;
}

/**
 * Loads what the form's guard needs up front, such as the reCAPTCHA script. Call it when the form is shown.
 */
export async function loadSubmissionGuard(definition: FormClientModel): Promise<void> {
    const guard = definition.submissionGuard && guards.get(definition.submissionGuard.alias);
    await guard?.load?.(settingsOf(definition));
}

export async function getSubmissionGuardValues(definition: FormClientModel, values: FormValues): Promise<Record<string, string>> {
    const guard = definition.submissionGuard && guards.get(definition.submissionGuard.alias);
    return guard ? guard.getValues(settingsOf(definition), values) : {};
}

// Render a hidden text input named settings.fieldName that people can't see, and keep its value in the form values under that
// name; only a bot fills it in
registerSubmissionGuard('honeypot', {
    async getValues(settings, values) {
        const fieldName = String(settings.fieldName ?? '');
        return fieldName ? { [fieldName]: String(values[fieldName] ?? '') } : {};
    }
});

interface ReCaptcha {
    ready(callback: () => void): void;
    execute(siteKey: string, options: { action: string }): Promise<string>;
}

function getReCaptcha(): ReCaptcha | undefined {
    return (globalThis as { grecaptcha?: ReCaptcha }).grecaptcha;
}

registerSubmissionGuard('recaptchaV3', {
    async load(settings) {
        if (getReCaptcha() || typeof document === 'undefined') return;

        await new Promise<void>((resolve, reject) => {
            const script = document.createElement('script');
            script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(String(settings.siteKey ?? ''))}`;
            script.async = true;
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('The reCAPTCHA script could not be loaded.'));
            document.head.appendChild(script);
        });
    },
    async getValues(settings) {
        await this.load!(settings);
        const recaptcha = getReCaptcha();
        if (!recaptcha) throw new Error('reCAPTCHA is not available.');

        await new Promise<void>(resolve => recaptcha.ready(resolve));
        const token = await recaptcha.execute(String(settings.siteKey ?? ''), { action: String(settings.action || 'submit') });
        return { 'g-recaptcha-response': token };
    }
});
