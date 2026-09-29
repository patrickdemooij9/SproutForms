import type { FormClientModel, HeadlessOutcome } from './api/types.gen';

export interface OutcomeContext {
    definition: FormClientModel;
}

/**
 * Shows the visitor the outcome of a submit, such as a message or a redirect in your router.
 */
export type OutcomeHandler = (outcome: HeadlessOutcome, context: OutcomeContext) => void | Promise<void>;

const handlers = new Map<string, OutcomeHandler>();

/**
 * Adds or replaces the handler for an outcome type: "message", "redirect", "redirectUmbracoPage" or one of your own.
 */
export function registerOutcomeHandler(type: string, handler: OutcomeHandler): void {
    handlers.set(type, handler);
}

/**
 * Runs the handler registered for the outcome's type. Returns false when there is none, or no outcome at all (the form's outcome
 * couldn't run); the submission is saved either way, so show definition.texts.submitSucceeded instead.
 */
export async function handleOutcome(outcome: HeadlessOutcome | null | undefined, context: OutcomeContext): Promise<boolean> {
    const handler = outcome && handlers.get(outcome.type);
    if (!outcome || !handler) return false;

    await handler(outcome, context);
    return true;
}
