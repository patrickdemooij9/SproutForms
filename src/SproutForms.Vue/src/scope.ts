import { getEntryPrefix, getEntryScope, getValueAtPath, type FormValues } from '@sproutforms/client';
import { computed, inject, provide, type ComputedRef, type InjectionKey } from 'vue';
import { useSproutFormContext } from './context';

/**
 * Where the fields being rendered are: the form itself, or an entry of a field group.
 */
export interface FieldScope {
    // What their paths start with: empty for the form's own fields, such as "people[0]." in an entry. An entry's index changes
    // when an entry before it is removed
    prefix: ComputedRef<string>;
    // The values their conditions see: an entry's own over the form's (see getEntryScope)
    values: ComputedRef<FormValues>;
}

const fieldScopeKey: InjectionKey<FieldScope> = Symbol('sproutform-field-scope');

/**
 * The scope of the fields rendered here: the entry this component is in, or the form.
 */
export function useFieldScope(): FieldScope {
    const scope = inject(fieldScopeKey, undefined);
    if (scope) return scope;

    const form = useSproutFormContext();
    return { prefix: computed(() => ''), values: computed(() => form.state.value.values) };
}

/**
 * Makes the fields rendered inside this component those of an entry of the field group at groupPath. A theme's
 * RepeaterEntry calls it before it renders the entry's rows.
 */
export function provideEntryScope(groupPath: () => string, index: () => number): FieldScope {
    const form = useSproutFormContext();
    const parent = useFieldScope();

    const scope: FieldScope = {
        prefix: computed(() => getEntryPrefix(groupPath(), index())),
        values: computed(() => {
            const entry = getValueAtPath(form.state.value.values, `${groupPath()}[${index()}]`);
            return getEntryScope((entry ?? {}) as FormValues, parent.values.value);
        })
    };
    provide(fieldScopeKey, scope);
    return scope;
}
