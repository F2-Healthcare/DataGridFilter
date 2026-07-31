using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace FilterDataGrid;

internal static class HeaderTextResolver
{
    public static string Resolve(DataGridColumn column, CultureInfo culture = null)
    {
        if (column is null) return string.Empty;

        var headerText = ResolveContent(column.Header);
        if (!string.IsNullOrWhiteSpace(headerText))
        {
            if (!string.IsNullOrEmpty(column.HeaderStringFormat))
                return string.Format(culture ?? CultureInfo.CurrentCulture, column.HeaderStringFormat, headerText);

            return headerText;
        }

        return column switch
        {
            DataGridTextColumn textColumn when !string.IsNullOrWhiteSpace(textColumn.FieldName) => textColumn.FieldName,
            DataGridTemplateColumn templateColumn when !string.IsNullOrWhiteSpace(templateColumn.FieldName) => templateColumn.FieldName,
            DataGridCheckBoxColumn checkBoxColumn when !string.IsNullOrWhiteSpace(checkBoxColumn.FieldName) => checkBoxColumn.FieldName,
            DataGridComboBoxColumn comboBoxColumn when !string.IsNullOrWhiteSpace(comboBoxColumn.FieldName) => comboBoxColumn.FieldName,
            _ when !string.IsNullOrWhiteSpace(column.SortMemberPath) => column.SortMemberPath,
            _ => string.Empty
        };
    }

    private static string ResolveContent(object content)
    {
        switch (content)
        {
            case null:
                return null;
            case string text:
                return text;
            case TextBlock textBlock:
                return textBlock.Text;
            case AccessText accessText:
                return accessText.Text;
            case Run run:
                return run.Text;
            case ContentControl contentControl when !ReferenceEquals(contentControl.Content, content):
                return ResolveContent(contentControl.Content);
            case ContentPresenter contentPresenter when !ReferenceEquals(contentPresenter.Content, content):
                return ResolveContent(contentPresenter.Content);
            case Decorator decorator:
                return ResolveContent(decorator.Child);
            case Panel panel:
                return string.Join(" ", panel.Children.Cast<UIElement>()
                    .Select(ResolveContent)
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            case FrameworkElement:
                return null;
            default:
                var value = content.ToString();
                return value == content.GetType().FullName ? null : value;
        }
    }
}
