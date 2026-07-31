using System.Collections.Generic;
using System.Windows;

namespace FilterDataGrid;

public class ItemsSourceMembers
{
    public string DisplayMember { get; set; }
    public string SelectedValue { get; set; }
}

public class DataGridCheckBoxColumn : System.Windows.Controls.DataGridCheckBoxColumn
{
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(
            nameof(FieldName),
            typeof(string),
            typeof(DataGridCheckBoxColumn),
            new PropertyMetadata(""));

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public static readonly DependencyProperty IsColumnFilteredProperty =
        DependencyProperty.Register(
            nameof(IsColumnFiltered),
            typeof(bool),
            typeof(DataGridCheckBoxColumn),
            new PropertyMetadata(false));

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }
}

public class DataGridComboBoxColumn : System.Windows.Controls.DataGridComboBoxColumn
{
    public List<ItemsSourceMembers> ComboBoxItemsSource { get; set; }
    public bool IsSingle { get; set; }

    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(
            nameof(FieldName),
            typeof(string),
            typeof(DataGridComboBoxColumn),
            new PropertyMetadata(""));

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public static readonly DependencyProperty IsColumnFilteredProperty =
        DependencyProperty.Register(
            nameof(IsColumnFiltered),
            typeof(bool),
            typeof(DataGridComboBoxColumn),
            new PropertyMetadata(false));

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }
}

public class DataGridTemplateColumn : System.Windows.Controls.DataGridTemplateColumn
{
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(
            nameof(FieldName),
            typeof(string),
            typeof(DataGridTemplateColumn),
            new PropertyMetadata(""));

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public static readonly DependencyProperty FieldTypeProperty =
        DependencyProperty.Register(
            nameof(FieldType),
            typeof(System.Type),
            typeof(DataGridTemplateColumn),
            new PropertyMetadata(null));

    /// <summary>
    ///     Type used to select the filter UI. When omitted, the type is inferred from <see cref="FieldName"/>.
    /// </summary>
    public System.Type FieldType
    {
        get => (System.Type)GetValue(FieldTypeProperty);
        set => SetValue(FieldTypeProperty, value);
    }

    public static readonly DependencyProperty IsColumnFilteredProperty =
        DependencyProperty.Register(
            nameof(IsColumnFiltered),
            typeof(bool),
            typeof(DataGridTemplateColumn),
            new PropertyMetadata(false));

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }
}

public class DataGridTextColumn : System.Windows.Controls.DataGridTextColumn
{
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(
            nameof(FieldName),
            typeof(string),
            typeof(DataGridTextColumn),
            new PropertyMetadata(""));

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public static readonly DependencyProperty IsColumnFilteredProperty =
        DependencyProperty.Register(
            nameof(IsColumnFiltered),
            typeof(bool),
            typeof(DataGridTextColumn),
            new PropertyMetadata(false));

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }
}
