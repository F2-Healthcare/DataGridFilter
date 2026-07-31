using System.Globalization;

namespace FilterDataGrid;

public class Loc
{
    public CultureInfo Culture { get; } = new("en-US");
    public string DisplayName => Culture.DisplayName;
    public string EnglishName => "English";

    public string All => "(Select all)";
    public string Cancel => "Cancel";
    public string Clear => "Clear filter \"{0}\"";
    public string Contains => "Search";
    public string Empty => "(Blank)";
    public string IsFalse => "Unchecked";
    public string IsTrue => "Checked";
    public string Neutral => "{0}";
    public string Ok => "Ok";
    public string RemoveAll => "Remove all filters";
    public string StartsWith => "Search (startswith)";
    public string Toggle => "Toggle contains/startswith";
    public string Indeterminate => "Indeterminate";
}
