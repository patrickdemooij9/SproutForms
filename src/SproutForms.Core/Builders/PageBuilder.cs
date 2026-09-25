using SproutForms.Core.Models;

namespace SproutForms.Core.Builders
{
    public class PageBuilder
    {
        private readonly FormBuilder _form;
        private readonly FormPage _page;

        internal PageBuilder(FormBuilder form, string? title)
        {
            _form = form;
            _page = new FormPage { Title = title };
        }

        public PageBuilder Row(Action<RowBuilder> configure)
        {
            var row = new RowBuilder(_form);
            configure(row);
            _page.Rows.Add(row.Build());
            return this;
        }

        public PageBuilder NextLabel(string label)
        {
            _page.NextLabel = label;
            return this;
        }

        public PageBuilder PreviousLabel(string label)
        {
            _page.PreviousLabel = label;
            return this;
        }

        /// <summary>
        /// Shows the page only when all the conditions hold, such as an answer on an earlier page. A hidden page is skipped, and its fields aren't validated.
        /// </summary>
        public PageBuilder VisibleWhen(Action<ConditionBuilder> configure)
        {
            var conditions = new ConditionBuilder();
            configure(conditions);
            _page.Visibility = conditions.Build();
            return this;
        }

        internal FormPage Build() => _page;
    }
}
