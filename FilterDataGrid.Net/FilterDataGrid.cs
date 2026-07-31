using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FilterDataGrid;

public class FilterDataGrid : DataGrid, INotifyPropertyChanged
{
    #region Constructors

    /// <summary>
    ///     FilterDataGrid constructor
    /// </summary>
    public FilterDataGrid()
    {
        Debug.WriteLineIf(DebugMode, "FilterDataGrid.Constructor");

        DefaultStyleKey = typeof(FilterDataGrid);

        // load resources
        var resourceDictionary = new ResourceDictionary
        {
            Source = new Uri("/FilterDataGrid;component/Themes/Generic.xaml", UriKind.Relative)
        };

        Resources.MergedDictionaries.Add(resourceDictionary);

        // initial popup size
        _popUpSize = new Point
        {
            X = (double)TryFindResource("PopupWidth"),
            Y = (double)TryFindResource("PopupHeight")
        };

        CommandBindings.Add(new CommandBinding(ApplyFilter, ApplyFilterCommand, CanApplyFilter)); // Ok
        CommandBindings.Add(new CommandBinding(CancelFilter, CancelFilterCommand));
        CommandBindings.Add(new CommandBinding(ClearSearchBox, ClearSearchBoxClick));
        CommandBindings.Add(new CommandBinding(IsChecked, CheckedAllCommand));
        CommandBindings.Add(new CommandBinding(RemoveAllFilters, RemoveAllFilterCommand, CanRemoveAllFilter));
        CommandBindings.Add(new CommandBinding(RemoveFilter, RemoveFilterCommand, CanRemoveFilter));
        CommandBindings.Add(new CommandBinding(ShowFilter, ShowFilterCommand, CanShowFilter));
    }

    #endregion Constructors

    #region Command

    public static readonly ICommand ApplyFilter = new RoutedCommand();
    public static readonly ICommand CancelFilter = new RoutedCommand();
    public static readonly ICommand ClearSearchBox = new RoutedCommand();
    public static readonly ICommand IsChecked = new RoutedCommand();
    public static readonly ICommand RemoveAllFilters = new RoutedCommand();
    public static readonly ICommand RemoveFilter = new RoutedCommand();
    public static readonly ICommand ShowFilter = new RoutedCommand();

    #endregion Command

    #region Public DependencyProperty

    /// <summary>
    ///     Excluded Fields (only AutoGeneratingColumn)
    /// </summary>
    public static readonly DependencyProperty ExcludeFieldsProperty =
        DependencyProperty.Register("ExcludeFields",
            typeof(string),
            typeof(FilterDataGrid),
            new PropertyMetadata(""));

    /// <summary>
    ///     Excluded Column (only AutoGeneratingColumn)
    /// </summary>
    public static readonly DependencyProperty ExcludeColumnsProperty =
        DependencyProperty.Register("ExcludeColumns",
            typeof(string),
            typeof(FilterDataGrid),
            new PropertyMetadata(""));

    /// <summary>
    ///     Date format displayed
    /// </summary>
    public static readonly DependencyProperty DateFormatStringProperty =
        DependencyProperty.Register("DateFormatString",
            typeof(string),
            typeof(FilterDataGrid),
            new PropertyMetadata("d"));

    /// <summary>
    ///     Filter popup background property.
    ///     Allows the user to set a custom background color for the filter popup. When nothing is set, the default value is background color of host windows.
    /// </summary>
    public static readonly DependencyProperty FilterPopupBackgroundProperty =
        DependencyProperty.Register("FilterPopupBackground",
            typeof(Brush),
            typeof(FilterDataGrid),
            new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty CollectionViewSourceProperty =
        DependencyProperty.Register("CollectionViewSource",
            typeof(ICollectionView),
            typeof(FilterDataGrid),
            new PropertyMetadata());

    #endregion Public DependencyProperty

    #region Public Event

    public event PropertyChangedEventHandler PropertyChanged;

    public event EventHandler Sorted;

    #endregion Public Event

    #region Private Fields

    private const bool DebugMode = false;

    private DataGridColumnHeadersPresenter _columnHeadersPresenter;
    private int _busyCount;
    private Cursor _cursorBeforeBusy;
    private bool _currentlyFiltering;
    private bool _isResizing;
    private bool _search;
    private Button _button;

    private Cursor _cursor;
    private int _searchLength;
    private double _minHeight;
    private double _minWidth;
    private double _sizableContentHeight;
    private double _sizableContentWidth;
    private Grid _sizableContentGrid;

    private List<string> _excludedFields;
    private List<string> _excludedColumns;
    private List<FilterItemDate> _treeView;
    private List<FilterItem> _listBoxItems;

    private Point _popUpSize;
    private Popup _popup;

    private string _fieldName;
    private string _lastFilter;
    private string _searchText;
    private TextBox _searchTextBox;
    private Thumb _thumb;

    private Type _collectionType;
    private Type _fieldType;

    private bool _startsWith;

    private readonly FilterEngine _filterEngine = new();

    #endregion Private Fields

    #region Public Properties

    /// <summary>
    ///     Excluded Fields (AutoGeneratingColumn)
    /// </summary>
    public string ExcludeFields
    {
        get => (string)GetValue(ExcludeFieldsProperty);
        set => SetValue(ExcludeFieldsProperty, value);
    }

    /// <summary>
    ///     Excluded Columns (AutoGeneratingColumn)
    /// </summary>
    public string ExcludeColumns
    {
        get => (string)GetValue(ExcludeColumnsProperty);
        set => SetValue(ExcludeColumnsProperty, value);
    }

    /// <summary>
    ///     The string begins with the specific character. Used in pop-up search box
    /// </summary>
    public bool StartsWith
    {
        get => _startsWith;
        set
        {
            _startsWith = value;
            OnPropertyChanged();

            // refresh filter
            if (!string.IsNullOrEmpty(_searchText)) _itemCollectionView.Refresh();
        }
    }

    /// <summary>
    ///     Date format displayed
    /// </summary>
    public string DateFormatString
    {
        get => (string)GetValue(DateFormatStringProperty);
        set => SetValue(DateFormatStringProperty, value);
    }

    /// <summary>
    ///     Instance of Loc
    /// </summary>
    public Loc Translate { get; private set; }

    /// <summary>
    /// Tree View ItemsSource
    /// </summary>
    public List<FilterItemDate> TreeViewItems
    {
        get => _treeView ?? [];
        set
        {
            _treeView = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// ListBox ItemsSource
    /// </summary>
    public List<FilterItem> ListBoxItems
    {
        get => _listBoxItems ?? [];
        set
        {
            _listBoxItems = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Field Type
    /// </summary>
    public Type FieldType
    {
        get => _fieldType;
        set
        {
            _fieldType = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Filter pop-up background
    /// </summary>
    public Brush FilterPopupBackground
    {
        get => (Brush)GetValue(FilterPopupBackgroundProperty);
        set => SetValue(FilterPopupBackgroundProperty, value);
    }

    public ICollectionView CollectionViewSource
    {
        get { return (ICollectionView)GetValue(CollectionViewSourceProperty); }
        set { SetValue(CollectionViewSourceProperty, value); }
    }

    #endregion Public Properties

    #region Private Properties

    private FilterCommon _currentFilter { get; set; }
    private ICollectionView _itemCollectionView { get; set; }
    private List<FilterCommon> _globalFilterList => _filterEngine.Filters;

    /// <summary>
    /// Popup filtered items (ListBox/TreeView)
    /// </summary>
    private IEnumerable<FilterItem> _popupViewItems =>
        _itemCollectionView?.OfType<FilterItem>().Where(c => c.Level != 0) ?? [];

    /// <summary>
    /// Popup source collection (ListBox/TreeView)
    /// </summary>
    private IEnumerable<FilterItem> _sourcePopupViewItems =>
        _itemCollectionView?.SourceCollection.OfType<FilterItem>().Where(c => c.Level != 0) ?? [];

    #endregion Private Properties

    #region Protected Methods

    // CALL ORDER :
    // Constructor
    // OnInitialized
    // OnItemsSourceChanged

    /// <summary>
    ///     Initialize datagrid
    /// </summary>
    /// <param name="e"></param>
    protected override void OnInitialized(EventArgs e)
    {
        Debug.WriteLineIf(DebugMode, "OnInitialized");

        base.OnInitialized(e);

        try
        {
            Translate = new Loc();

            // fill excluded Fields list with values
            if (AutoGenerateColumns)
            {
                _excludedFields = ExcludeFields.Split(',').Select(p => p.Trim()).ToList();
                _excludedColumns = ExcludeColumns.Split(',').Select(p => p.Trim()).ToList();
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnInitialized : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Auto generated column, set templateHeader
    /// </summary>
    /// <param name="e"></param>
    protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
    {
        Debug.WriteLineIf(DebugMode, "OnAutoGeneratingColumn");

        base.OnAutoGeneratingColumn(e);

        try
        {
            if (_excludedColumns.Any(x => string.Equals(x, e.PropertyName, StringComparison.CurrentCultureIgnoreCase)))
            // ignore excluded columns
            {
                e.Cancel = true;
                return;
            }

            // enable column sorting when user specified
            e.Column.CanUserSort = CanUserSortColumns;

            // return if the field is excluded
            if (_excludedFields.Any(c => string.Equals(c, e.PropertyName, StringComparison.CurrentCultureIgnoreCase))) return;

            // template
            var template = (DataTemplate)TryFindResource("DataGridHeaderTemplate");

            // get type
            _fieldType = Nullable.GetUnderlyingType(e.PropertyType) ?? e.PropertyType;

            if (_fieldType.IsEnum)
            {
                var column = new DataGridComboBoxColumn
                {
                    ItemsSource = ((System.Windows.Controls.DataGridComboBoxColumn)e.Column).ItemsSource,
                    SelectedItemBinding = new Binding(e.PropertyName),
                    FieldName = e.PropertyName,
                    Header = e.Column.Header,
                    HeaderTemplate = template,
                    IsSingle = false, // eNum is not a unique value (unique identifier)
                    IsColumnFiltered = true
                };

                e.Column = column;
            }
            //else if (_fieldType == typeof(bool))
            //{
            //    var column = new DataGridCheckBoxColumn
            //    {
            //        Binding = new Binding(e.PropertyName) { ConverterCulture = Translate.Culture },
            //        FieldName = e.PropertyName,
            //        Header = e.Column.Header,
            //        HeaderTemplate = template,
            //        IsColumnFiltered = true
            //    };

            //    e.Column = column;
            //}
            else
            {
                var column = new DataGridTextColumn
                {
                    Binding = new Binding(e.PropertyName) { ConverterCulture = Translate.Culture },
                    FieldName = e.PropertyName,
                    Header = e.Column.Header,
                    IsColumnFiltered = true
                };

                if (IsDateFieldType(_fieldType))
                {
                    //var cellStyle = e.Column.CellStyle;
                    //cellStyle ??= new Style();

                    //var horizontalAlignment = new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                    //cellStyle.Setters.Add(horizontalAlignment);
                    //column.CellStyle = cellStyle;

                    // apply the format string provided
                    column.Binding.StringFormat = DateFormatString;
                }

                if (e.PropertyType == typeof(decimal))
                {
                    //var cellStyle = e.Column.CellStyle;
                    //cellStyle ??= new Style();

                    //var horizontalAlignment = new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                    //cellStyle.Setters.Add(horizontalAlignment);
                    //column.CellStyle = cellStyle;

                    column.Binding.StringFormat = "$#,##0.00";
                }

                if (e.PropertyType == typeof(int) || e.PropertyType == typeof(short))
                {
                    //var cellStyle = e.Column.CellStyle;
                    //cellStyle ??= new Style();

                    //var horizontalAlignment = new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                    //cellStyle.Setters.Add(horizontalAlignment);
                    //column.CellStyle = cellStyle;
                }

                // if the type does not belong to the "System" namespace, disable sorting
                if (!_fieldType.IsSystemType())
                {
                    column.CanUserSort = false;

                    // if the type is a nested object (class), disable cell editing
                    column.IsReadOnly = _fieldType.IsClass;
                }
                else
                {
                    column.HeaderTemplate = template;
                }

                e.Column = column;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnAutoGeneratingColumn : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Use the visible header text when WPF creates the clipboard header row.
    ///     UIElement headers otherwise use their type name (for example,
    ///     System.Windows.Controls.TextBlock).
    /// </summary>
    protected override void OnCopyingRowClipboardContent(DataGridRowClipboardEventArgs e)
    {
        base.OnCopyingRowClipboardContent(e);

        if (e.IsColumnHeadersRow)
        {
            for (var index = 0; index < e.ClipboardRowContent.Count; index++)
            {
                var cell = e.ClipboardRowContent[index];
                e.ClipboardRowContent[index] = new DataGridClipboardCellContent(
                    cell.Item,
                    cell.Column,
                    HeaderTextResolver.Resolve(cell.Column));
            }
        }
    }

    /// <summary>
    ///     The source of the Data grid items has been changed (refresh or on loading)
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        Debug.WriteLineIf(DebugMode, $"\nOnItemsSourceChanged Auto : {AutoGenerateColumns}");

        base.OnItemsSourceChanged(oldValue, newValue);

        try
        {
            if (newValue is null)
            {
                RemoveFilters();

                // remove custom HeaderTemplate
                foreach (var col in Columns)
                {
                    col.HeaderTemplate = null;
                }
                return;
            }

            if (oldValue is not null)
            {
                RemoveFilters();

                // free previous resource
                CollectionViewSource = System.Windows.Data.CollectionViewSource.GetDefaultView(new object());

                // scroll to top on reload collection
                var scrollViewer = GetTemplateChild("DG_ScrollViewer") as ScrollViewer;
                scrollViewer?.ScrollToTop();
            }

            CollectionViewSource = System.Windows.Data.CollectionViewSource.GetDefaultView(ItemsSource);

            // set Filter, contribution : STEFAN HEIMEL
            if (CollectionViewSource.CanFilter) CollectionViewSource.Filter = Filter;

            OnPropertyChanged(nameof(_globalFilterList));

            // get collection type
            // contribution : APFLKUACHA
            _collectionType = ItemsSource is ICollectionView collectionView
                ? collectionView.SourceCollection?.GetType().GenericTypeArguments.FirstOrDefault()
                : ItemsSource?.GetType().GenericTypeArguments.FirstOrDefault();

            // generating custom columns
            if (!AutoGenerateColumns && _collectionType is not null) GeneratingCustomsColumn();

            // re-evalutate the command's CanExecute.
            // when "IsReadOnly" is set to "False", "CanRemoveAllFilter" is not re-evaluated,
            // the "Remove All Filters" icon remains active
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnItemsSourceChanged : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Set the cursor to "Cursors.Wait" during a long sorting operation
    ///     https://stackoverflow.com/questions/8416961/how-can-i-be-notified-if-a-datagrid-column-is-sorted-and-not-sorting
    /// </summary>
    /// <param name="eventArgs"></param>
    protected override void OnSorting(DataGridSortingEventArgs eventArgs)
    {
        if (_currentlyFiltering || (_popup?.IsOpen ?? false)) return;

        BeginBusy();
        try
        {
            base.OnSorting(eventArgs);
            Sorted?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            EndBusy();
        }
    }

    #endregion Protected Methods

    #region Public Methods

    /// <summary>
    ///     Remove All Filters
    /// </summary>
    public void RemoveFilters()
    {
        Debug.WriteLineIf(DebugMode, "RemoveFilters");

        try
        {
            foreach (var column in _globalFilterList.Select(filter => filter.Column)
                         .Where(column => column is not null))
            {
                FilterState.SetIsFiltered(column, false);
            }

            // reset current filter
            _currentFilter = null;
            _filterEngine.Clear();
            CollectionViewSource?.Refresh();
        }
        catch (Exception ex)
        {
            Debug.WriteLineIf(DebugMode, $"RemoveFilters error : {ex.Message}");
            throw;
        }
    }

    #endregion Public Methods

    #region Private Methods

    private static bool IsDateFieldType(Type type)
    {
        return type == typeof(DateTime) || type == typeof(DateOnly);
    }

    private static DateOnly ToDateOnly(object value)
    {
        return value switch
        {
            DateOnly date => date,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => throw new ArgumentException("Value must be a DateTime or DateOnly.", nameof(value))
        };
    }

    /// <summary>
    ///     Build the item tree
    /// </summary>
    /// <param name="dates"></param>
    /// <returns></returns>
    private List<FilterItemDate> BuildTree(IEnumerable<FilterItem> dates)
    {
        try
        {
            var tree = new List<FilterItemDate>
            {
                new() {
                    Label = Translate.All,
                    Level = 0,
                    Initialize = true,
                    FieldType = _fieldType
                }
            };

            if (dates is null) return tree;

            // iterate over all items that are not null
            // INFO:
            // Initialize   : does not call the SetIsChecked method
            // IsChecked    : call the SetIsChecked method
            // (see the FilterItem class for more information)

            var dateTimes = dates.ToList();

            foreach (var y in dateTimes.Where(c => c.Level == 1)
                         .Select(filterItem => new
                         {
                             Date = ToDateOnly(filterItem.Content),
                             Item = filterItem
                         })
                         .GroupBy(g => g.Date.Year)
                         .Select(year => new FilterItemDate
                         {
                             Level = 1,
                             Content = year.Key,
                             Label = year.FirstOrDefault()?.Date.ToString("yyyy", Translate.Culture),
                             Initialize = true, // default state
                             FieldType = _fieldType,

                             Children = year.GroupBy(date => date.Date.Month)
                                 .Select(month => new FilterItemDate
                                 {
                                     Level = 2,
                                     Content = month.Key,
                                     Label = month.FirstOrDefault()?.Date.ToString("MMMM", Translate.Culture),
                                     Initialize = true, // default state
                                     FieldType = _fieldType,

                                     Children = month.GroupBy(date => date.Date.Day)
                                         .Select(day => new FilterItemDate
                                         {
                                             Level = 3,
                                             Content = day.Key,
                                             Label = day.FirstOrDefault()?.Date.ToString("dd", Translate.Culture),
                                             Initialize = true, // default state
                                             FieldType = _fieldType,

                                             // filter Item linked to the day, it propagates the states changes
                                             Item = day.FirstOrDefault()?.Item,

                                             Children = []
                                         }).ToList()
                                 }).ToList()
                         }))
            {
                // set parent and IsChecked property if uncheck Previous items
                y.Children.ForEach(m =>
                {
                    m.Parent = y;

                    m.Children.ForEach(d =>
                    {
                        d.Parent = m;

                        // set the state of the "IsChecked" property based on the items already filtered (unchecked)
                        if (d.Item.IsChecked) return;

                        // call the SetIsChecked method of the FilterItemDate class
                        d.IsChecked = false;

                        // reset with new state (isChanged == false)
                        d.Initialize = d.IsChecked;
                    });
                    // reset with new state
                    m.Initialize = m.IsChecked;
                });
                // reset with new state
                y.Initialize = y.IsChecked;
                tree.Add(y);
            }

            // last empty item if exist in collection
            if (dateTimes.Any(d => d.Level == -1))
            {
                var empty = dateTimes.FirstOrDefault(x => x.Level == -1);
                if (empty is not null)
                    tree.Add(
                        new FilterItemDate
                        {
                            Label = Translate.Empty, // translation
                            Content = null,
                            Level = -1,
                            FieldType = _fieldType,
                            Initialize = empty.IsChecked,
                            Item = empty,
                            Children = []
                        }
                    );
            }

            tree.First().Tree = tree;
            return tree;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FilterCommon.BuildTree : {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Handle Mousedown, contribution : WORDIBOI
    /// </summary>
    private readonly MouseButtonEventHandler _onMousedown = (_, eArgs) => { eArgs.Handled = true; };

    /// <summary>
    ///     Generate custom columns that can be filtered
    /// </summary>
    private void GeneratingCustomsColumn()
    {
        Debug.WriteLineIf(DebugMode, "GeneratingCustomColumn");

        try
        {
            // get the columns that can be filtered
            // ReSharper disable MergeIntoPattern
            var columns = Columns
                .Where(c => (c is DataGridTextColumn dtx && dtx.IsColumnFiltered)
                            || (c is DataGridTemplateColumn dtp && dtp.IsColumnFiltered)
                            || (c is DataGridCheckBoxColumn dcb && dcb.IsColumnFiltered)
                            || (c is DataGridComboBoxColumn dbx && dbx.IsColumnFiltered)
                )
                .Select(c => c)
                .ToList();

            // set header template
            foreach (var col in columns)
            {
                var columnType = col.GetType();

                if (col.HeaderTemplate is not null)
                {
                    // Debug.WriteLineIf(DebugMode, "\tReset filter Button");

                    // The state belongs to the column so it survives header virtualization.
                    FilterState.SetIsFiltered(col, false);

                    // reset the "ComboBoxItemsSource" custom property of "DataGridComboBoxColumn"
                    // this collection may change when loading a new source collection of the DataGrid.
                    if (columnType == typeof(DataGridComboBoxColumn))
                        ((DataGridComboBoxColumn)col).ComboBoxItemsSource = null;
                }
                else
                {
                    Debug.WriteLineIf(DebugMode, "\tGenerate Columns");

                    _fieldType = null;
                    var template = (DataTemplate)TryFindResource("DataGridHeaderTemplate");

                    switch (columnType)
                    {
                        case { } when columnType == typeof(DataGridTemplateColumn):
                            // DataGridTemplateColumn has no culture property
                            var templateColumn = col as DataGridTemplateColumn;

                            if (string.IsNullOrEmpty(templateColumn.FieldName))
                                throw new ArgumentException("Value of \"FieldName\" property cannot be null.",
                                    nameof(DataGridTemplateColumn));
                            // template
                            templateColumn.HeaderTemplate = template;
                            break;

                        case { } when columnType == typeof(DataGridTextColumn):
                            var textColumn = col as DataGridTextColumn;

                            textColumn.FieldName = ((Binding)textColumn.Binding).Path.Path;

                            // template
                            textColumn.HeaderTemplate = template;

                            _fieldType = PropertyPathAccessor.GetPathType(_collectionType, textColumn.FieldName);

                            // apply DateFormatString when StringFormat for column is not provided or empty
                            if (IsDateFieldType(_fieldType) && !string.IsNullOrEmpty(DateFormatString))
                                if (string.IsNullOrEmpty(textColumn.Binding.StringFormat))
                                    textColumn.Binding.StringFormat = DateFormatString;

                            FieldType = _fieldType;

                            // culture
                            //if (((Binding)textColumn.Binding).ConverterCulture is null)
                            //    ((Binding)textColumn.Binding).ConverterCulture = Translate.Culture;
                            break;

                        case { } when columnType == typeof(DataGridCheckBoxColumn):
                            var checkBoxColumn = col as DataGridCheckBoxColumn;

                            checkBoxColumn.FieldName = ((Binding)checkBoxColumn.Binding).Path.Path;

                            // template
                            checkBoxColumn.HeaderTemplate = template;

                            // culture
                            if (((Binding)checkBoxColumn.Binding).ConverterCulture is null)
                                ((Binding)checkBoxColumn.Binding).ConverterCulture = Translate.Culture;
                            break;

                        case { } when columnType == typeof(DataGridComboBoxColumn):
                            var comboBoxColumn = col as DataGridComboBoxColumn;

                            if (comboBoxColumn.ItemsSource is null) return;

                            var binding = (Binding)comboBoxColumn.SelectedValueBinding ?? (Binding)comboBoxColumn.SelectedItemBinding;

                            // check if binding is missing
                            if (binding is not null)
                            {
                                comboBoxColumn.FieldName = binding.Path.Path;

                                // template
                                comboBoxColumn.HeaderTemplate = template;

                                _fieldType = PropertyPathAccessor.GetPathType(_collectionType, comboBoxColumn.FieldName);

                                // check if it is a unique id type and not nested object
                                comboBoxColumn.IsSingle = _fieldType?.IsSystemType() == true;

                                // culture
                                binding.ConverterCulture ??= Translate.Culture;
                            }
                            else
                            {
                                throw new ArgumentException(
                                    "Value of \"SelectedValueBinding\" property or \"SelectedItemBinding\" cannot be null.",
                                    nameof(DataGridComboBoxColumn));
                            }
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GeneratingCustomColumn : {ex.Message}");
            throw;
        }
    }

    private void BeginBusy()
    {
        if (_busyCount++ != 0)
            return;

        _cursorBeforeBusy = Cursor;
        SetCurrentValue(CursorProperty, Cursors.Wait);
        Mouse.UpdateCursor();
    }

    private async Task BeginBusyAsync()
    {
        BeginBusy();

        // Let WPF render the busy state before starting dispatcher-bound work.
        await Dispatcher.Yield(DispatcherPriority.Render);
    }

    private void EndBusy()
    {
        if (_busyCount == 0 || --_busyCount != 0)
            return;

        SetCurrentValue(CursorProperty, _cursorBeforeBusy);
        _cursorBeforeBusy = null;
        Mouse.UpdateCursor();
    }

    /// <summary>
    ///     Can Apply filter (popup Ok button)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CanApplyFilter(object sender, CanExecuteRoutedEventArgs e)
    {
        // CanExecute only when the popup is open
        if ((_popup?.IsOpen ?? false) == false)
        {
            e.CanExecute = false;
        }
        else
        {
            if (_search)
                e.CanExecute = _popupViewItems.Any(f => f?.IsChecked == true);
            else
                e.CanExecute = _popupViewItems.Any(f => f.IsChanged) &&
                               _popupViewItems.Any(f => f?.IsChecked == true);
        }
    }

    /// <summary>
    ///     Cancel button, close popup
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CancelFilterCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (_popup is null) return;
        _popup.IsOpen = false; // raise EventArgs PopupClosed
    }

    /// <summary>
    /// Can remove all filter when filters are active
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CanRemoveAllFilter(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _filterEngine.HasFilters;
    }

    /// <summary>
    ///     Can remove filter when current column is filtered
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CanRemoveFilter(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _currentFilter?.IsFiltered ?? false;
    }

    /// <summary>
    ///     Can show filter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CanShowFilter(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CollectionViewSource?.CanFilter == true && (!_popup?.IsOpen ?? true) && !_currentlyFiltering;
    }

    /// <summary>
    ///     Check/uncheck all item when the action is (select all)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CheckedAllCommand(object sender, ExecutedRoutedEventArgs e)
    {
        var item = (FilterItem)e.Parameter;

        // only when the item[0] (select all) is checked or unchecked
        if (item?.Level != 0 || _itemCollectionView is null) return;

        foreach (var obj in _popupViewItems.ToList()
                     .Where(f => f.IsChecked != item.IsChecked))
            obj.IsChecked = item.IsChecked;
    }

    /// <summary>
    ///     Clear Search Box text
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="routedEventArgs"></param>
    private void ClearSearchBoxClick(object sender, RoutedEventArgs routedEventArgs)
    {
        _search = false;
        _searchTextBox.Text = string.Empty; // raises TextChangedEventArgs
    }

    /// <summary>
    ///     Aggregate list of predicate as filter
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    private bool Filter(object o)
    {
        return _filterEngine.Filter(o);
    }

    /// <summary>
    ///     OnPropertyChange
    /// </summary>
    /// <param name="propertyName"></param>
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    ///     On Resize Thumb Drag Completed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnResizeThumbDragCompleted(object sender, DragCompletedEventArgs e)
    {
        Cursor = _cursor;
        _isResizing = false;
    }

    /// <summary>
    ///     Get delta on drag thumb
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        // initialize the first Actual size Width/Height
        if (_sizableContentHeight <= 0)
        {
            _sizableContentHeight = _sizableContentGrid.ActualHeight;
            _sizableContentWidth = _sizableContentGrid.ActualWidth;
        }

        var yAdjust = _sizableContentGrid.Height + e.VerticalChange;
        var xAdjust = _sizableContentGrid.Width + e.HorizontalChange;

        //make sure not to resize to negative width or height
        xAdjust = _sizableContentGrid.ActualWidth + xAdjust > _minWidth ? xAdjust : _minWidth;
        yAdjust = _sizableContentGrid.ActualHeight + yAdjust > _minHeight ? yAdjust : _minHeight;

        xAdjust = xAdjust < _minWidth ? _minWidth : xAdjust;
        yAdjust = yAdjust < _minHeight ? _minHeight : yAdjust;

        // set size of grid
        _sizableContentGrid.Width = xAdjust;
        _sizableContentGrid.Height = yAdjust;
    }

    /// <summary>
    ///     On Resize Thumb DragStarted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnResizeThumbDragStarted(object sender, DragStartedEventArgs e)
    {
        _cursor = Cursor;
        _isResizing = true;
        Cursor = Cursors.SizeNWSE;
    }

    /// <summary>
    ///     Reset the size of popup to original size
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PopupClosed(object sender, EventArgs e)
    {
        Debug.WriteLineIf(DebugMode, "PopupClosed");

        var pop = (Popup)sender;

        // free the resources if the popup is closed without filtering
        if (!_currentlyFiltering)
        {
            _currentFilter = null;
            _itemCollectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(new object());
            EndBusy();
        }

        pop.Closed -= PopupClosed;
        pop.MouseDown -= _onMousedown;

        _searchTextBox.TextChanged -= SearchTextBoxOnTextChanged;

        _thumb.DragCompleted -= OnResizeThumbDragCompleted;
        _thumb.DragDelta -= OnResizeThumbDragDelta;
        _thumb.DragStarted -= OnResizeThumbDragStarted;

        if (_sizableContentGrid is not null)
        {
            _sizableContentGrid.Width = _sizableContentWidth;
            _sizableContentGrid.Height = _sizableContentHeight;
        }

        if (_isResizing)
        {
            Cursor = _cursor;
            _isResizing = false;
        }

        // once the popup is closed, this is no longer necessary
        ListBoxItems = [];
        TreeViewItems = [];

        // re-enable columnHeadersPresenter
        if (_columnHeadersPresenter is not null)
            _columnHeadersPresenter.IsEnabled = true;
    }

    /// <summary>
    ///     Remove All Filter Command
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void RemoveAllFilterCommand(object sender, ExecutedRoutedEventArgs e)
    {
        await RemoveAllFiltersAsync();
    }

    private async Task RemoveAllFiltersAsync()
    {
        await BeginBusyAsync();
        try
        {
            RemoveFilters();
        }
        finally
        {
            EndBusy();
        }
    }

    /// <summary>
    ///     Remove current filter
    /// </summary>
    private async Task RemoveCurrentFilterAsync()
    {
        Debug.WriteLineIf(DebugMode, "RemoveCurrentFilter");

        if (_currentFilter is null) return;

        var filter = _currentFilter;
        _currentlyFiltering = true;
        try
        {
            if (_popup is not null)
                _popup.IsOpen = false; // raise PopupClosed event

            // reset button icon
            if (filter.Column is not null)
                FilterState.SetIsFiltered(filter.Column, false);

            await BeginBusyAsync();

            if (_filterEngine.Remove(filter))
                CollectionViewSource.Refresh();

            // set the last filter applied
            _lastFilter = _globalFilterList.LastOrDefault()?.FieldName;
            _currentFilter = null;
        }
        finally
        {
            _currentlyFiltering = false;
            _currentFilter = null;
            _itemCollectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(new object());
            EndBusy();
        }
    }

    /// <summary>
    ///     Remove Current Filter Command
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void RemoveFilterCommand(object sender, ExecutedRoutedEventArgs e)
    {
        await RemoveCurrentFilterAsync();
    }

    /// <summary>
    ///     Apply the filter to the items in the popup List/Treeview
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private bool SearchFilter(object obj)
    {
        var item = (FilterItem)obj;
        if (string.IsNullOrEmpty(_searchText) || item is null || item.Level == 0) return true;

        var content = Convert.ToString(item.Content, Translate.Culture);

        // Contains
        if (!StartsWith)
            return Translate.Culture.CompareInfo.IndexOf(content ?? string.Empty, _searchText,
                CompareOptions.OrdinalIgnoreCase) >= 0;

        // StartsWith preserve RangeOverflow
        if (_searchLength > item.ContentLength) return false;

        return Translate.Culture.CompareInfo.IndexOf(content ?? string.Empty, _searchText, 0, _searchLength,
            CompareOptions.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    ///     Search TextBox Text Changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SearchTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
        e.Handled = true;
        var textBox = (TextBox)sender;

        // fix TextChanged event fires twice I did not find another solution
        if (textBox is null || textBox.Text == _searchText || _itemCollectionView is null) return;

        _searchText = textBox.Text;

        _searchLength = _searchText.Length;

        _search = !string.IsNullOrEmpty(_searchText);

        // apply filter (call the SearchFilter method)
        _itemCollectionView.Refresh();

        if (!IsDateFieldType(_currentFilter.FieldType) || _treeView is null) return;

        // rebuild treeView
        if (string.IsNullOrEmpty(_searchText))
        {
            // populate the tree with items from the source list
            TreeViewItems = BuildTree(_sourcePopupViewItems);
        }
        else
        {
            // searchText is not empty
            // populate the tree only with items found by the search
            var items = _popupViewItems.Where(i => i.IsChecked).ToList();

            // if at least one element is not null, fill the tree, otherwise the tree contains only the element (select all).
            TreeViewItems = BuildTree(items.Count != 0 ? items : null);
        }
    }

    /// <summary>
    ///     Open a pop-up window, Click on the header button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ShowFilterCommand(object sender, ExecutedRoutedEventArgs e)
    {
        Debug.WriteLineIf(DebugMode, "\r\nShowFilterCommand");

        // clear search text (!important)
        _searchText = string.Empty;
        _search = false;

        try
        {
            // filter button
            _button = (Button)e.OriginalSource;

            if (Items.Count == 0 || _button is null) return;

            // contribution : OTTOSSON
            // for the moment this functionality is not tested, I do not know if it can cause unexpected effects
            _ = CommitEdit(DataGridEditingUnit.Row, true);

            // navigate up to the current header and get column type
            var header = VisualTreeHelpers.FindAncestor<DataGridColumnHeader>(_button);
            var headerColumn = header.Column;

            // then down to the current popup
            _popup = VisualTreeHelpers.FindChild<Popup>(header, "FilterPopup");
            _columnHeadersPresenter = VisualTreeHelpers.FindAncestor<DataGridColumnHeadersPresenter>(header);

            if (_popup is null || _columnHeadersPresenter is null) return;

            // disable columnHeadersPresenter while popup is open
            _columnHeadersPresenter.IsEnabled = false;

            // popup handle event
            _popup.Closed += PopupClosed;

            // disable popup background click-through, contribution : WORDIBOI
            _popup.MouseDown += _onMousedown;

            // resizable grid
            _sizableContentGrid = VisualTreeHelpers.FindChild<Grid>(_popup.Child, "SizableContentGrid");

            // Popup content is hosted in a separate visual tree, so give it an
            // explicit context instead of relying on ancestor bindings.
            if (_popup.Child is FrameworkElement popupContent)
                popupContent.SetCurrentValue(DataContextProperty, this);

            // Column header containers are recycled when column virtualization is enabled.
            // Stamp popup-specific values from the clicked column instead of binding back
            // through a header container that may already represent another column.
            var clearFilterButton = VisualTreeHelpers.FindChild<Button>(_popup.Child, "ClearFilterBnt");
            clearFilterButton?.SetCurrentValue(ContentControl.ContentProperty,
                HeaderTextResolver.Resolve(headerColumn));

            var popupBorder = VisualTreeHelpers.FindChild<Border>(_popup.Child, "PopUpBorder");
            popupBorder?.SetCurrentValue(Border.BackgroundProperty,
                FilterPopupBackground ?? Background ?? Brushes.White);

            // search textbox
            _searchTextBox = VisualTreeHelpers.FindChild<TextBox>(_popup.Child, "SearchBox");
            _thumb = VisualTreeHelpers.FindChild<Thumb>(_sizableContentGrid, "PopupThumb");

            _searchTextBox.Text = string.Empty;
            _searchTextBox.Focusable = true;
            _searchTextBox.TextChanged += SearchTextBoxOnTextChanged;

            _thumb.DragCompleted += OnResizeThumbDragCompleted;
            _thumb.DragDelta += OnResizeThumbDragDelta;
            _thumb.DragStarted += OnResizeThumbDragStarted;

            // minimum size of Grid
            _sizableContentHeight = 0;
            _sizableContentWidth = 0;

            _sizableContentGrid.Height = _popUpSize.Y;
            _sizableContentGrid.MinHeight = _popUpSize.Y;

            _minHeight = _sizableContentGrid.MinHeight;
            _minWidth = _sizableContentGrid.MinWidth;

            List<FilterItem> filterItemList = null;
            DataGridComboBoxColumn comboxColumn = null;

            // get field name from binding Path
            switch (headerColumn)
            {
                case DataGridTextColumn textColumn:
                    _fieldName = textColumn.FieldName;
                    break;
                case DataGridTemplateColumn templateColumn:
                    _fieldName = templateColumn.FieldName;
                    break;
                case DataGridCheckBoxColumn checkBoxColumn:
                    _fieldName = checkBoxColumn.FieldName;
                    break;
                case DataGridComboBoxColumn comboBoxColumn:
                    _fieldName = comboBoxColumn.FieldName;
                    comboxColumn = comboBoxColumn;

                    // Generates the list from "ItemsSource" of the combobox column, this will essentially be used to provide
                    // the filter labels for this type of column.
                    // Only for a column linked by an identifier(like ID) from any other collection (ItemsSource).

                    if (comboxColumn.IsSingle && comboxColumn.ComboBoxItemsSource is null)
                        comboxColumn.ComboBoxItemsSource = new List<ItemsSourceMembers>(comboxColumn.ItemsSource
                            .Cast<object>()
                            .Select(x =>
                                new ItemsSourceMembers
                                {
                                    SelectedValue = Convert.ToString(PropertyPathAccessor.GetValue(x, comboxColumn.SelectedValuePath), CultureInfo.InvariantCulture),
                                    DisplayMember = Convert.ToString(PropertyPathAccessor.GetValue(x, comboxColumn.DisplayMemberPath), Translate.Culture)
                                }))
                            .ToList();
                    break;
            }

            // invalid fieldName
            if (string.IsNullOrEmpty(_fieldName)) return;

            var firstItemWithValue = Items.Cast<object>().FirstOrDefault(item =>
                PropertyPathAccessor.GetValue(item, _fieldName) is not null);
            var inferredFieldType = PropertyPathAccessor.GetPathType(_collectionType, _fieldName) ??
                                    PropertyPathAccessor.GetValue(firstItemWithValue, _fieldName)?.GetType();
            var configuredFieldType = (headerColumn as DataGridTemplateColumn)?.FieldType;
            FieldType = Nullable.GetUnderlyingType(configuredFieldType ?? inferredFieldType) ??
                        configuredFieldType ?? inferredFieldType;

            _currentFilter = _filterEngine.GetOrCreateFilter(_fieldName, FieldType);
            _currentFilter.FieldType = FieldType;
            _currentFilter.Column = headerColumn;

            await BeginBusyAsync();

            // list for all items values, filtered and unfiltered (previous filtered items)
            List<object> sourceObjectList;

            // get the list of raw values of the current column
            if (_fieldType == typeof(DateTime))
            {
                // possible distinct values because time part is removed
                sourceObjectList = Items.Cast<object>()
                    .Select(x => (object)((DateTime?)PropertyPathAccessor.GetValue(x, _fieldName))?.Date)
                    .Distinct()
                    .ToList();
            }
            else
            {
                sourceObjectList = Items.Cast<object>()
                    .Select(x => PropertyPathAccessor.GetValue(x, _fieldName))
                    .Distinct()
                    .ToList();
            }

            // adds the previous filtered items to the list of new items (currentFilter.PreviouslyFilteredItems)
            if (_lastFilter == _currentFilter.FieldName)
                sourceObjectList.AddRange(_currentFilter?.PreviouslyFilteredItems ?? []);

            // empty item flag
            // if they exist, remove all null or empty string values from the list.
            // content is null and content == "" are two different things but both labeled as (blank)
            var emptyItem = sourceObjectList.RemoveAll(v => v is null || v.Equals(string.Empty)) > 0;

            // TODO : AggregateException when user can add row

            // Sorting detached values is safe to move off the dispatcher and keeps the window responsive.
            var unsortedValues = sourceObjectList;
            sourceObjectList = await Task.Run(() => unsortedValues.AsParallel().OrderBy(x => x).ToList());

            if (_fieldType == typeof(bool))
                filterItemList = new List<FilterItem>(sourceObjectList.Count + 1);
            else
                // add the first element (select all) at the top of list
                filterItemList = new List<FilterItem>(sourceObjectList.Count + 2)
                {
                    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident (compatibility with Net4.8)
                    new() { Label = Translate.All, IsChecked = true, Level = 0 }
                };

            // add all items (not null) to the filterItemList,
            // the list of dates is calculated by BuildTree from this list
            filterItemList.AddRange(sourceObjectList.Select(item => new FilterItem
            {
                Content = item,
                ContentLength = item?.ToString().Length ?? 0,
                FieldType = _fieldType,
                Label = GetLabel(item, _fieldType),
                Level = 1,
                Initialize = _currentFilter.PreviouslyFilteredItems?.Contains(item) == false
            }));

            // add a empty item(if exist) at the bottom of the list
            if (emptyItem)
            {
                sourceObjectList.Insert(sourceObjectList.Count, null);

                filterItemList.Add(new FilterItem
                {
                    FieldType = _fieldType,
                    Content = null,
                    Label = _fieldType == typeof(bool) ? Translate.Indeterminate : Translate.Empty,
                    Level = -1,
                    Initialize = _currentFilter?.PreviouslyFilteredItems?.Contains(null) == false
                });
            }

            string GetLabel(object o, Type type)
            {
                string label;

                // retrieve the label of the list previously reconstituted from "ItemsSource" of the combobox
                if (comboxColumn?.IsSingle == true)
                    label = comboxColumn.ComboBoxItemsSource
                        ?.FirstOrDefault(x => x.SelectedValue == o.ToString())?.DisplayMember;
                else
                    // label of other columns
                    label = type != typeof(bool) ? o.ToString()
                        // translates boolean value label
                        : o is not null && (bool)o ? Translate.IsTrue : Translate.IsFalse;

                return label;
            }

            // ItemsSource (ListBow/TreeView)
            if (IsDateFieldType(_fieldType))
                TreeViewItems = BuildTree(filterItemList);
            else
                ListBoxItems = filterItemList;

            // Set ICollectionView for filtering in the pop-up window
            _itemCollectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(filterItemList);

            // set filter in popup
            if (_itemCollectionView.CanFilter) _itemCollectionView.Filter = SearchFilter;

            // set the placement and offset of the PopUp in relation to the header and the main window of the application
            // i.e (placement : bottom left or bottom right)
            PopupPlacement(_sizableContentGrid, header);

            _popup.UpdateLayout();

            // open popup
            _popup.IsOpen = true;

            // set focus on searchTextBox
            _searchTextBox.Focus();
            Keyboard.Focus(_searchTextBox);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ShowFilterCommand error : {ex.Message}");
            throw;
        }
        finally
        {
            EndBusy();
        }
    }

    /// <summary>
    ///     Click OK Button when Popup is Open, apply filter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ApplyFilterCommand(object sender, ExecutedRoutedEventArgs e)
    {
        Debug.WriteLineIf(DebugMode, "\r\nApplyFilterCommand");

        _currentlyFiltering = true;

        try
        {
            if (_popup is null || _currentFilter is null)
                return;

            _popup.IsOpen = false; // raise PopupClosed event
            await BeginBusyAsync();

            var isFiltered = _filterEngine.ApplyPopupSelection(
                _currentFilter,
                _search,
                _popupViewItems.ToList(),
                _sourcePopupViewItems.ToList());

            // set the current field name as the last filter name
            _lastFilter = isFiltered ? _currentFilter.FieldName : _globalFilterList.LastOrDefault()?.FieldName;

            // Apply or clear the filter with exactly one view refresh.
            CollectionViewSource.Refresh();

            // set button icon (filtered or not)
            if (_currentFilter.Column is not null)
                FilterState.SetIsFiltered(_currentFilter.Column, isFiltered);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyFilterCommand error : {ex.Message}");
            throw;
        }
        finally
        {
            // free resources (unsubscribe from the event and re-enable "columnHeadersPresenter"
            // is done in PopupClosed method)
            _currentlyFiltering = false;
            _currentFilter = null;
            _itemCollectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(new object());
            EndBusy();
        }
    }

    /// <summary>
    ///     PopUp placement and offset
    /// </summary>
    /// <param name="grid"></param>
    /// <param name="header"></param>
    private void PopupPlacement(FrameworkElement grid, FrameworkElement header)
    {
        try
        {
            _popup.PlacementTarget = header;
            _popup.HorizontalOffset = 0d;
            _popup.VerticalOffset = -1d;
            _popup.Placement = PlacementMode.Bottom;

            // get the host window of the datagrid, contribution : STEFAN HEIMEL
            var hostingWindow = Window.GetWindow(this);

            if (hostingWindow is null) return;

            const double border = 1d;

            // get the ContentPresenter from the hostingWindow
            var contentPresenter = VisualTreeHelpers.FindChild<ContentPresenter>(hostingWindow);

            var hostSize = new Point
            {
                X = contentPresenter.ActualWidth,
                Y = contentPresenter.ActualHeight
            };

            // get the X, Y position of the header
            var headerContentOrigin = header.TransformToVisual(contentPresenter).Transform(new Point(0, 0));
            var headerDataGridOrigin = header.TransformToVisual(this).Transform(new Point(0, 0));

            var headerSize = new Point { X = header.ActualWidth, Y = header.ActualHeight };
            var offset = _popUpSize.X - headerSize.X + border;

            // the popup must stay in the DataGrid, move it to the left of the header, because it overflows on the right.
            if (headerDataGridOrigin.X + headerSize.X > _popUpSize.X) _popup.HorizontalOffset -= offset;

            // delta for max size popup
            var delta = new Point
            {
                X = hostSize.X - (headerContentOrigin.X + headerSize.X),
                Y = hostSize.Y - (headerContentOrigin.Y + headerSize.Y + _popUpSize.Y)
            };

            // max size
            grid.MaxWidth = MaxSize(_popUpSize.X + delta.X - border);
            grid.MaxHeight = MaxSize(_popUpSize.Y + delta.Y - border);

            // remove offset
            // contributing to the fix : VASHBALDEUS
            if (_popup.HorizontalOffset == 0)
                grid.MaxWidth = MaxSize(Math.Abs(grid.MaxWidth - offset));

            if (!(delta.Y <= 0d)) return;

            // the height of popup is too large, reduce it, because it overflows down.
            grid.MaxHeight = MaxSize(_popUpSize.Y - Math.Abs(delta.Y) - border);
            grid.Height = grid.MaxHeight;

            // contributing to the fix : VASHBALDEUS
            grid.MinHeight = grid.MaxHeight == 0 ? grid.MinHeight : grid.MaxHeight;

            // greater than or equal to 0.0
            static double MaxSize(double size)
            {
                return size >= 0.0d ? size : 0.0d;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PopupPlacement error : {ex.Message}");
            throw;
        }
    }

    #endregion Private Methods
}
