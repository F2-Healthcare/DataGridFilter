using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace FilterDataGrid;

public sealed class FilterCommon : NotifyProperty
{
    public HashSet<object> PreviouslyFilteredItems { get; set; } = new HashSet<object>(EqualityComparer<object>.Default);

    public string FieldName { get; set; }

    public DataGridColumn Column { get; set; }

    public Button FilterButton { get; set; }

    public Type FieldType { get; set; }

    public bool IsFiltered
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsFiltered));
        }
    }
}
