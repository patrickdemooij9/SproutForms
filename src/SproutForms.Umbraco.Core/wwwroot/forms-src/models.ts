export interface SubmissionGuard {
    load?: (form: HTMLFormElement, settings: Record<string, unknown>) => Promise<void>;
    beforeSubmit?: (form: HTMLFormElement, settings: Record<string, unknown>, payload: FormData) => Promise<void>;
}

export interface SubmissionGuardRegistry {
    [alias: string]: SubmissionGuard;
}

export interface FieldCondition {
    rules: Array<{
        fieldAlias: string;
        comparison: string;
        value: unknown;
    }>;
    operator?: "All" | "Any";
}

export interface FieldConditions {
    visibility?: FieldCondition;
    required?: FieldCondition;
}

export interface Validator {
    (value: string | undefined, options: Record<string, string | undefined>, context?: Element): Promise<boolean>;
}

export interface ValidatorRegistry {
    [alias: string]: Validator;
}

export interface ValidationResult {
    valid: boolean;
    rule?: string;
    message?: string;
}

export interface SproutFormsCore {
    submissionGuard: {
        registry: SubmissionGuardRegistry;
        register(alias: string, guard: SubmissionGuard): void;
    };
    conditions: {
        registry: FieldCondition[] | null;
        init(form: Element): void;
        evaluate(fieldConditions: FieldCondition | undefined, formValues: Record<string, unknown>): boolean;
    };
    validation: {
        registry: ValidatorRegistry;
        register(alias: string, validator: Validator): void;
        validateField(fieldContainer: Element): Promise<ValidationResult>;
    };
}

export interface GuardSettings {
    siteKey?: string;
    action?: string;
}

export interface GuardDefinition {
    alias: string;
    settings: Record<string, unknown>;
}

export interface FormSubmitResult {
    successMessage?: string;
    redirectUrl?: string;
    errors?: Record<string, string[]>;
    values?: Record<string, unknown>;
}
