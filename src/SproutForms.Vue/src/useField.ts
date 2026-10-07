import { isFieldRequired, isFieldVisible, type FormClientField } from '@sproutforms/client';
import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useSproutFormContext } from './context';

/**
 * Everything a field wrapper needs for one field of the form it is in: its state, and its control with the props to bind on it.
 * A wrapper of your own then only does the layout:
 *
 * ```vue
 * const { id, visible, required, errors, control, controlProps } = useField(() => props.field);
 *
 * <div :hidden="!visible || undefined">
 *     <label :for="id">{{ field.label }}</label>
 *     <component :is="control" v-bind="controlProps" />
 * </div>
 * ```
 */
export function useField(field: MaybeRefOrGetter<FormClientField>) {
    const form = useSproutFormContext();

    // Errors and values are keyed by it
    const path = computed(() => toValue(field).alias);
    const id = computed(() => form.getFieldId(path.value));
    const errorId = computed(() => `${id.value}-error`);

    const visible = computed(() => isFieldVisible(toValue(field), form.state.value.values, form.state.value.variables));
    const required = computed(() => isFieldRequired(toValue(field), form.state.value.values, form.state.value.variables));
    const errors = computed(() => form.state.value.errors[path.value] ?? []);
    const invalid = computed(() => errors.value.length > 0);
    const value = computed(() => form.state.value.values[path.value]);
    const disabled = computed(() => form.state.value.status === 'submitting');

    // The form's override for the field's alias, or its type's control in the form's theme; undefined for a type without one
    const control = computed(() => form.resolveField(toValue(field)));

    function setValue(next: unknown): void {
        form.engine.setValue(path.value, next);
    }

    // fieldControlProps and the update handler, to v-bind on the control
    const controlProps = computed(() => ({
        id: id.value,
        field: toValue(field),
        modelValue: value.value,
        path: path.value,
        required: required.value,
        invalid: invalid.value,
        errors: errors.value,
        disabled: disabled.value,
        describedBy: invalid.value ? errorId.value : undefined,
        'onUpdate:modelValue': setValue
    }));

    return { path, id, errorId, value, setValue, visible, required, errors, invalid, disabled, control, controlProps };
}

export type UseFieldReturn = ReturnType<typeof useField>;
