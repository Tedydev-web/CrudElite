using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper.Execution;
using CrudElite.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CrudElite.Helpers;

public static class TableHelper
{
    public static List<SelectListItem> DefaultPageSizeOptions = GetPageSizeDropDownList(10);

    public static ColumnHeader[] GenerateDefaultColumnHeaders<T>(string defaultSortOrder, List<ColumnHeader> TableColumns) where T : class
    {
        var headers = new List<ColumnHeader>();
        var count = 1;

        foreach (var property in TableColumns)
        {
            var columnHeader = new ColumnHeader();
            columnHeader.Index = count;
            columnHeader.Key = property.Title;
            var parameter = Expression.Parameter(typeof(T), "s");

            var propertyParts = property.Title.Split('.');

            if (propertyParts.Length > 1)
            {
                // The property is nested (example: StudentViewModel.StudentNameModel.Value), so we need to generate a chain of property expressions
                Expression memberExpression = parameter;

                foreach (var part in propertyParts)
                    memberExpression = Expression.Property(memberExpression, part);
                var displayAttribute = (DisplayAttribute)memberExpression.GetMemberExpressions().FirstOrDefault().Member
                                                                         .GetCustomAttributes(typeof(DisplayAttribute), false).SingleOrDefault();

                if (displayAttribute != null)
                {
                    var displayName = displayAttribute.GetName();
                    columnHeader.Title = displayName;
                }
            }
            else
            {
                var memberExpression = Expression.Property(parameter, property.Title);
                var displayAttribute = (DisplayAttribute)memberExpression.Member.GetCustomAttributes(typeof(DisplayAttribute), true)
                                                                         .FirstOrDefault();
                var propertyName = displayAttribute != null ? displayAttribute.Name : property.Title;
                var displayName = propertyName;
                columnHeader.Title = displayName;
            }

            if (defaultSortOrder.Contains(property.Title))
            {
                var segments = defaultSortOrder.Split('-');
                var order = segments.Last();
                columnHeader.OrderAction = order == "desc" ? $"{property}-asc" : $"{property}-desc";
            }
            else
                columnHeader.OrderAction = $"{property}-desc";
            columnHeader.Sortable = property.Sortable;
            columnHeader.Exportable = property.Exportable;

            headers.Add(columnHeader);
            count++;
        }

        return headers.ToArray();
    }

    public static List<ColumnHeader> GetColumnHeaders(ColumnHeader[] DefaultColumnHeaders, string sort)
    {
        var headers = new List<ColumnHeader>();

        foreach (var header in DefaultColumnHeaders)
        {
            if (!string.IsNullOrEmpty(header.OrderAction))
                header.OrderAction = sort == $"{header.Key}-asc" ? $"{header.Key}-desc" : $"{header.Key}-asc";
            headers.Add(header);
        }

        return headers;
    }

    public static List<SelectListItem> GetPageSizeDropDownList(int? defaultValue)
    {
        var pageSizeDropDownList = new List<SelectListItem>
        {
            new() { Text = "Show 10 Records", Value = "10", Selected = defaultValue == 10 ? true : false },
            new() { Text = "Show 25 Records", Value = "25", Selected = defaultValue == 25 ? true : false },
            new() { Text = "Show 50 Records", Value = "50", Selected = defaultValue == 50 ? true : false },
            new() { Text = "Show All", Value = "-1", Selected = defaultValue == -1 ? true : false }
        };

        return pageSizeDropDownList;
    }

    public static IQueryable<T> PerformSort<T>(IQueryable<T> list, string defaultSortOrder, string sort) where T : class
    {
        if (string.IsNullOrEmpty(sort))
            sort = defaultSortOrder;

        var segments = sort.Split('-');
        var column = segments[0];
        var direction = segments.Length > 1 ? segments[1] : "desc";

        PropertyInfo propertyInfo;
        Expression propertyExpression;
        var parameter = Expression.Parameter(typeof(T), "s");

        propertyInfo = typeof(T).GetProperty(column);
        propertyExpression = Expression.Property(parameter, propertyInfo);

        var lambda = Expression.Lambda(propertyExpression, parameter);

        var methodName = direction == "desc" ? "OrderByDescending" : "OrderBy";

        var result = typeof(Queryable).GetMethods()
                                      .Single(
                                          method => method.Name == methodName &&
                                                    method.IsGenericMethodDefinition &&
                                                    method.GetGenericArguments().Length == 2 &&
                                                    method.GetParameters().Length == 2)
                                      .MakeGenericMethod(typeof(T), propertyInfo.PropertyType)
                                      .Invoke(null, new object[] { list, lambda });

        return (IQueryable<T>)result;
    }
}

public class ClientTableConfig
{
    public static readonly List<ColumnHeader> TableColumns = new()
    {
        new() { Title = nameof(ClientViewModel.Name), Sortable = true, Exportable = true },
        new() { Title = nameof(ClientViewModel.EmailAddress), Sortable = true, Exportable = true },
        new() { Title = nameof(ClientViewModel.Notes), Sortable = true, Exportable = true }
    };

    public static bool ShowActionColumn = true;

    public static string SearchMessage = "Search...";

    public static string DefaultSortOrder = $"{nameof(ClientViewModel.Name)}-asc";
    public static int? DefaultPageSize = 10;
    public static readonly ColumnHeader[] DefaultColumnHeaders = TableHelper.GenerateDefaultColumnHeaders<ClientViewModel>(DefaultSortOrder, TableColumns);

    public static IQueryable<ClientViewModel> PerformSearch(IQueryable<ClientViewModel> list, string search)
    {
        if (!string.IsNullOrEmpty(search))
        {
            search = search.TrimStart().TrimEnd();
            list = list.Where(s => s.Name.Contains(search) || s.EmailAddress.Contains(search) || s.Notes.Contains(search));
        }

        return list;
    }

    public static IQueryable<ClientViewModel> PerformSort(IQueryable<ClientViewModel> list, string sort)
    {
        var result = TableHelper.PerformSort(list, DefaultSortOrder, sort);

        return result;
    }
}