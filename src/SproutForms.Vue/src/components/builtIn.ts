import type { Component } from 'vue';
import type { FormComponents } from '../themes';
import Actions from './default/Actions.vue';
import Errors from './default/Errors.vue';
import Field from './default/Field.vue';
import Form from './default/Form.vue';
import Rows from './default/Rows.vue';
import Success from './default/Success.vue';
import CheckboxField from './fields/CheckboxField.vue';
import EmailField from './fields/EmailField.vue';
import HiddenField from './fields/HiddenField.vue';
import SelectField from './fields/SelectField.vue';
import TextareaField from './fields/TextareaField.vue';
import TextField from './fields/TextField.vue';

/**
 * What every theme falls back to.
 */
export const builtInComponents: FormComponents = { Form, Rows, Field, Actions, Errors, Success };

export const builtInFields: Record<string, Component> = {
    text: TextField,
    email: EmailField,
    textarea: TextareaField,
    select: SelectField,
    checkbox: CheckboxField,
    hidden: HiddenField
};
