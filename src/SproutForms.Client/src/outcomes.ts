import type { FormClientModel, HeadlessOutcome } from './api/types.gen';
import { Registry, type Lookup } from './registry';

export interface OutcomeContext {
    definition: FormClientModel;
}

/**
 * Shows the visitor the outcome of a submit, such as a message or a redirect in your router. A renderer can pass a context with
 * more in it, such as a way to show the message in place of the form.
 */
export type OutcomeHandler<TContext extends OutcomeContext = OutcomeContext> = (outcome: HeadlessOutcome, context: TContext) => void | Promise<void>;

/**
 * The outcome handlers every form uses unless its own registry replaces them. None are registered here: what an outcome looks
 * like is up to the renderer.
 */
export const globalOutcomeHandlers = new Registry<OutcomeHandler>();

/**
 * Adds or replaces the handler for an outcome type: "message", "redirect", "redirectUmbracoPage" or one of your own.
 */
export function registerOutcomeHandler(type: string, handler: OutcomeHandler): void {
    globalOutcomeHandlers.register(type, handler);
}

/**
 * Runs the handler registered for the outcome's type. Returns false when there is none, or no outcome at all (the form's outcome
 * couldn't run); the submission is saved either way, so show definition.texts.submitSucceeded instead.
 */
export async function handleOutcome<TContext extends OutcomeContext>(
    outcome: HeadlessOutcome | null | undefined,
    context: TContext,
    handlers: Lookup<OutcomeHandler<TContext>> = globalOutcomeHandlers
): Promise<boolean> {
    const handler = outcome && handlers.get(outcome.type);
    if (!outcome || !handler) return false;

    await handler(outcome, context);
    return true;
}
