import type { FormClientField } from '@sproutforms/client';
import type { ExtractPublicPropTypes, PropType } from 'vue';

/**
 * The props every field type's control gets from the field wrapper. Use them in your own control with
 * `defineProps(fieldControlProps)` and emit `update:modelValue` with the new value.
 */
export const fieldControlProps = {
    field: { type: Object as PropType<FormClientField>, required: true },
    modelValue: { type: null as unknown as PropType<unknown>, default: undefined },
    // Unique on the page; the wrapper's label points at it
    id: { type: String, required: true },
    // The field's alias, or its path in a field group's entry such as "people[0].email"; errors are keyed by it
    path: { type: String, required: true },
    // Required on its own or because one of its rules holds right now
    required: { type: Boolean, default: false },
    invalid: { type: Boolean, default: false },
    errors: { type: Array as PropType<string[]>, default: () => [] },
    disabled: { type: Boolean, default: false },
    // The id of the element with the field's errors, while it has any
    describedBy: { type: String, default: undefined }
} as const;

export type FieldControlProps = ExtractPublicPropTypes<typeof fieldControlProps>;
