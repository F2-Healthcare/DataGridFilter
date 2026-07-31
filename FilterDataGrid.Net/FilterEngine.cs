using System;
using System.Collections.Generic;
using System.Linq;

namespace FilterDataGrid;

internal sealed class FilterEngine
{
    private readonly Dictionary<string, Predicate<object>> _criteria = [];

    public List<FilterCommon> Filters { get; } = [];

    public bool HasFilters => Filters.Count > 0;

    public bool Filter(object item)
    {
        foreach (var predicate in _criteria.Values)
            if (!predicate(item))
                return false;

        return true;
    }

    public FilterCommon GetOrCreateFilter(string fieldName, Type fieldType)
    {
        return Filters.FirstOrDefault(f => f.FieldName == fieldName) ??
               new FilterCommon
               {
                   FieldName = fieldName,
                   FieldType = fieldType
               };
    }

    public void Clear()
    {
        _criteria.Clear();
        Filters.Clear();
    }

    public bool ApplyPopupSelection(
        FilterCommon currentFilter,
        bool search,
        IEnumerable<FilterItem> popupViewItems,
        IEnumerable<FilterItem> sourcePopupViewItems)
    {
        var previousFiltered = currentFilter.PreviouslyFilteredItems;
        var blankIsChanged = new FilterItem();

        if (search)
        {
            // In search mode, the item (blank) is always unchecked.
            blankIsChanged.IsChecked = false;
            blankIsChanged.IsChanged = !previousFiltered.Any(c => c is not null && c.Equals(string.Empty));

            var searchResult = popupViewItems.Where(c => c.IsChecked).ToList();
            var uncheckedItems = sourcePopupViewItems.Except(searchResult).ToList();
            uncheckedItems.AddRange(searchResult.Where(c => c.IsChecked == false));

            previousFiltered.ExceptWith(searchResult.Select(c => c.Content));
            previousFiltered.UnionWith(uncheckedItems.Select(c => c.Content));
        }
        else
        {
            var changedItems = popupViewItems.Where(c => c.IsChanged).ToList();
            var checkedItems = changedItems.Where(c => c.IsChecked);
            var uncheckedItems = changedItems.Where(c => !c.IsChecked).ToList();

            previousFiltered.ExceptWith(checkedItems.Select(c => c.Content));
            previousFiltered.UnionWith(uncheckedItems.Select(c => c.Content));

            blankIsChanged.IsChecked = changedItems.Any(c => c.Level == -1 && c.IsChecked);
            blankIsChanged.IsChanged = changedItems.Any(c => c.Level == -1);
        }

        ApplyBlankStringChange(currentFilter, blankIsChanged);

        if (previousFiltered.Count == 0)
        {
            Remove(currentFilter);
            return false;
        }

        AddOrUpdateFilter(currentFilter);
        return true;
    }

    public void AddOrUpdateFilter(FilterCommon filter)
    {
        if (!filter.IsFiltered)
            AddPredicate(filter);

        if (Filters.All(f => f.FieldName != filter.FieldName))
            Filters.Add(filter);
    }

    public bool Remove(FilterCommon filter)
    {
        var removedCriteria = filter.IsFiltered && _criteria.Remove(filter.FieldName);

        Filters.Remove(filter);
        filter.IsFiltered = false;

        return removedCriteria;
    }

    private void AddPredicate(FilterCommon filter)
    {
        Func<object, object> valueAccessor = null;

        _criteria.Add(filter.FieldName, Predicate);
        filter.IsFiltered = true;
        return;

        bool Predicate(object item)
        {
            if (item is null)
                return false;

            valueAccessor ??= PropertyPathAccessor.GetAccessor(item.GetType(), filter.FieldName);
            var rawValue = valueAccessor(item);
            var value = filter.FieldType == typeof(DateTime)
                ? ((DateTime?)rawValue)?.Date
                : rawValue;

            return !filter.PreviouslyFilteredItems.Contains(value);
        }
    }

    private static void ApplyBlankStringChange(FilterCommon currentFilter, FilterItem blankIsChanged)
    {
        if (!blankIsChanged.IsChanged || currentFilter.FieldType != typeof(string))
            return;

        var previousFiltered = currentFilter.PreviouslyFilteredItems;

        switch (blankIsChanged.IsChecked)
        {
            case false:
                previousFiltered.Add(string.Empty);
                break;

            case true when previousFiltered.Any(c => c?.ToString() == string.Empty):
                previousFiltered.RemoveWhere(item => item?.ToString() == string.Empty);
                break;
        }
    }
}
