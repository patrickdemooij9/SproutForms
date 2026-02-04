using SproutForms.Core.Fields;
using SproutForms.Core.Fields.Configs;
using SproutForms.Core.Models;
using SproutForms.Core.Models.Outcomes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SproutForms.Core.Builders
{
    public static class FormBuilderExtensions
    {
        public static FieldBuilder<TextFieldConfig, string> Text(
        this ColumnBuilder column,
        string alias,
        string label)
        {
            var fieldType = new TextFieldFormFieldType();
            return column.Field<TextFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<EmailFieldConfig, string> Email(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new EmailFieldType();
            return column.Field<EmailFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<TextAreaConfig, string> Textarea(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new TextAreaFieldType();
            return column.Field<TextAreaConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<CheckboxFieldConfig, bool> Checkbox(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new CheckboxFieldType();
            return column.Field<CheckboxFieldConfig, bool>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<SelectFieldConfig, string> Select(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new SelectFieldType();
            return column.Field<SelectFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<HiddenFieldConfig, string> Hidden(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new HiddenFieldType();
            return column.Field<HiddenFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<RadioFieldConfig, string> Radio(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new RadioFieldType();
            return column.Field<RadioFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<DateFieldConfig, string> Date(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new DateFieldType();
            return column.Field<DateFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<FileFieldConfig, string> File(
            this ColumnBuilder column,
            string alias,
            string label)
        {
            var fieldType = new FileFieldType();
            return column.Field<FileFieldConfig, string>(alias, label, fieldType, fieldType.DefaultConfiguration);
        }

        public static FieldBuilder<TConfig, TValue> Field<TConfig, TValue>(
        this ColumnBuilder column,
        string alias,
        string label,
        IFormFieldType fieldType,
        TConfig config) where TConfig : class
        {
            var field = new FormField
            {
                Alias = alias,
                Label = label,
                FieldTypeAlias = fieldType.Alias,
                Configuration = config,
            };

            column.Form.RegisterField(field);

            return new FieldBuilder<TConfig, TValue>(
                column, field, config);
        }

        public static FormBuilder ThankYouMessage(
        this FormBuilder builder,
        string message)
        {
            return builder.SetOutcome(
                new ShowMessageOutcome(), new ShowMessageOutcomeConfig() { Message = message });
        }

        public static FormBuilder RedirectTo(
            this FormBuilder builder,
            string url)
        {
            return builder.SetOutcome(
                new RedirectUrlOutcomeType(), new RedirectUrlOutcomeConfig() { RedirectUrl = url });
        }
    }
}
