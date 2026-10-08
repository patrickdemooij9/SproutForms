import type { Component } from 'vue';
import type { FormComponents } from '../themes';
import Actions from './default/Actions.vue';
import Errors from './default/Errors.vue';
import Field from './default/Field.vue';
import Form from './default/Form.vue';
import Progress from './default/Progress.vue';
import RepeaterEntry from './default/RepeaterEntry.vue';
import Rows from './default/Rows.vue';
import SubmissionGuard from './default/SubmissionGuard.vue';
import Success from './default/Success.vue';
import CheckboxField from './fields/CheckboxField.vue';
import DateField from './fields/DateField.vue';
import EmailField from './fields/EmailField.vue';
import FileField from './fields/FileField.vue';
import HiddenField from './fields/HiddenField.vue';
import RadioField from './fields/RadioField.vue';
import RepeaterField from './fields/RepeaterField.vue';
import SelectField from './fields/SelectField.vue';
import TextareaField from './fields/TextareaField.vue';
import TextField from './fields/TextField.vue';

/**
 * What every theme falls back to.
 */
export const builtInComponents: FormComponents = { Form, Progress, Rows, Field, RepeaterEntry, Actions, Errors, SubmissionGuard, Success };

export const builtInFields: Record<string, Component> = {
    text: TextField,
    email: EmailField,
    textarea: TextareaField,
    select: SelectField,
    checkbox: CheckboxField,
    radio: RadioField,
    date: DateField,
    file: FileField,
    hidden: HiddenField,
    repeater: RepeaterField
};
