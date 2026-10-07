import type { FormClientField, FormClientModel, FormEngine, FormState } from '@sproutforms/client';
import { inject, type Component, type InjectionKey, type Ref } from 'vue';
import type { FormComponentName } from './themes';

/**
 * What a form shows once it is submitted: the outcome's message as HTML from the CMS, or the form's plain thank-you text.
 */
export interface FormSuccess {
    message?: string;
    text?: string;
}

/**
 * One rendered form, as the components of a theme see it.
 */
export interface SproutFormContext {
    definition: FormClientModel;
    engine: FormEngine;
    // The engine's latest state; read it in a computed or template to update with it
    state: Readonly<Ref<FormState>>;
    // Set once the form is submitted and its outcome shows a message rather than going elsewhere
    success: Readonly<Ref<FormSuccess | undefined>>;
    // The theme the form renders with
    theme: string;
    // The id of a field's control: unique on the page, and the same on the server and in the browser
    getFieldId(path: string): string;
    // The control for a field: the form's override for its alias, or its type's in the theme
    resolveField(field: FormClientField): Component | undefined;
    resolveComponent(name: FormComponentName): Component;
    // Validates the form, submits it when it is valid and handles the outcome
    submit(): Promise<void>;
}

export const sproutFormKey: InjectionKey<SproutFormContext> = Symbol('sproutform');

/**
 * The form this component is in. Use it in your own theme components.
 */
export function useSproutFormContext(): SproutFormContext {
    const form = inject(sproutFormKey, undefined);
    if (!form) throw new Error('useSproutFormContext() only works in a component inside <SproutForm>.');
    return form;
}
