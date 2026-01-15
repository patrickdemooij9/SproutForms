using SproutForms.Core.Models;
using System.Text.Json;

namespace SproutForms.Core.Builders
{
    public class FieldBuilder<TConfig, TValue> where TConfig: class
    {
        private readonly ColumnBuilder _column;
        private readonly FormField _field;
        private readonly TConfig _config;

        internal FieldBuilder(
            ColumnBuilder column,
            FormField field,
            TConfig config)
        {
            _column = column;
            _field = field;
            _config = config;
        }

        public FieldBuilder<TConfig, TValue> Required()
        {
            _field.Required = true;
            return this;
        }

        public FieldBuilder<TConfig, TValue> Set(Action<TConfig> configurate)
        {
            configurate(_config);
            _field.Configuration = _config;
            return this;
        }

        public ColumnBuilder Done()
        {
            _column.SetField(_field);
            return _column;
        }

        public TConfig Config => _config;
    }
}
