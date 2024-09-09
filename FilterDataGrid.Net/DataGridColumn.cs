﻿using System.Collections.Generic;
using System.Windows;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace

namespace FilterDataGrid;


public class ItemsSourceMembers
{
    #region Public Properties

    public string DisplayMember { get; set; }
    public string SelectedValue { get; set; }

    #endregion Public Properties
}

public sealed class DataGridCheckBoxColumn : System.Windows.Controls.DataGridCheckBoxColumn
{
    #region Public Fields

    /// <summary>
    /// FieldName Dependency Property.
    /// </summary>
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(DataGridCheckBoxColumn),
            new PropertyMetadata(""));

    /// <summary>
    /// IsColumnFiltered Dependency Property.
    /// </summary>
    public static readonly DependencyProperty IsColumnFilteredProperty =
        DependencyProperty.Register(nameof(IsColumnFiltered), typeof(bool), typeof(DataGridCheckBoxColumn),
            new PropertyMetadata(false));

    #endregion Public Fields

    #region Public Properties

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }

    #endregion Public Properties
}

public sealed class DataGridComboBoxColumn : System.Windows.Controls.DataGridComboBoxColumn
{
    #region Public Properties

    public List<ItemsSourceMembers> ComboBoxItemsSource { get; set; }
    public bool IsSingle { get; set; }

    #endregion Public Properties

    #region Public Fields

    /// <summary>
    /// FieldName Dependency Property.
    /// </summary>
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(DataGridComboBoxColumn),
            new PropertyMetadata(""));

    /// <summary>
    /// IsColumnFiltered Dependency Property.
    /// </summary>
    public static readonly DependencyProperty IsColumnFilteredProperty =
        DependencyProperty.Register(nameof(IsColumnFiltered), typeof(bool), typeof(DataGridComboBoxColumn),
            new PropertyMetadata(false));

    #endregion Public Fields

    #region Public Properties

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }

    #endregion Public Properties
}

public sealed class DataGridTemplateColumn : System.Windows.Controls.DataGridTemplateColumn
{
    #region Public Fields

    /// <summary>
    /// FieldName Dependency Property.
    /// </summary>
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(DataGridTemplateColumn),
            new PropertyMetadata(""));

    /// <summary>
    /// IsColumnFiltered Dependency Property.
    /// </summary>
    public static readonly DependencyProperty IsColumnFilteredProperty =
                DependencyProperty.Register(nameof(IsColumnFiltered), typeof(bool), typeof(DataGridTemplateColumn),
            new PropertyMetadata(false));

    #endregion Public Fields

    #region Public Properties

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }

    #endregion Public Properties
}

public sealed class DataGridTextColumn : System.Windows.Controls.DataGridTextColumn
{
    #region Public Fields

    /// <summary>
    /// FieldName Dependency Property.
    /// </summary>
    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(DataGridTextColumn),
            new PropertyMetadata(""));

    /// <summary>
    /// IsColumnFiltered Dependency Property.
    /// </summary>
    public static readonly DependencyProperty IsColumnFilteredProperty =
                DependencyProperty.Register(nameof(IsColumnFiltered), typeof(bool), typeof(DataGridTextColumn),
            new PropertyMetadata(false));

    #endregion Public Fields

    #region Public Properties

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public bool IsColumnFiltered
    {
        get => (bool)GetValue(IsColumnFilteredProperty);
        set => SetValue(IsColumnFilteredProperty, value);
    }

    #endregion Public Properties
}
