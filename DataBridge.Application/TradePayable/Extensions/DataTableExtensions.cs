using System.Data;
using System.Reflection;

namespace DataBridge.Application.TradePayable.Extensions;

public static class DataTableExtensions
{
    /// <summary>
    /// Maps each DataRow to T using property-name matching with type coercion.
    /// Mirrors how Dapper maps SQL result sets — used when reading a DataTable
    /// as a typed entity sequence instead of loading from the DB.
    /// </summary>



    //public static IEnumerable<T> ToEnumerable<T>(this DataTable table) where T : new()
    //{
    //    var props = typeof(T)
    //        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //        .Where(p => p.CanWrite)
    //        .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    //    foreach (DataRow row in table.Rows)
    //    {
    //        var item = new T();
    //        foreach (DataColumn col in table.Columns)
    //        {
    //            if (!props.TryGetValue(col.ColumnName, out var prop) || row.IsNull(col)) continue;
    //            try
    //            {
    //                var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
    //                prop.SetValue(item, Convert.ChangeType(row[col], target));
    //            }
    //            catch (Exception ex)
    //            { /* type mismatch between DataTable column and entity property — leave default */ }

    //        }
    //        yield return item;
    //    }
    //}


    public static IEnumerable<T> ToEnumerable<T>(this DataTable table)
    where T : new()
    {
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (DataRow row in table.Rows)
        {
            var item = new T();
            foreach (DataColumn col in table.Columns)
            {
                if (!props.TryGetValue(col.ColumnName, out var prop)
                    || row.IsNull(col)) continue;

                var rawValue = row[col]?.ToString();
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType)
                                 ?? prop.PropertyType;

                // Handle null/empty targeting Nullable types
                if (Nullable.GetUnderlyingType(prop.PropertyType) != null
                    && string.IsNullOrWhiteSpace(rawValue))
                {
                    prop.SetValue(item, null);
                    continue;
                }

                try
                {
                    object convertedValue;

                    if (targetType == typeof(Guid))
                    {
                        if (Guid.TryParse(rawValue, out var guidVal))
                        {
                            convertedValue = guidVal;
                        }
                        else
                        {
                            throw new FormatException($"String '{rawValue}' is not a valid GUID for column {col.ColumnName}.");
                        }
                    }
                    else if (targetType == typeof(bool))
                    {
                        // Handle true/false, 1/0, and yes/no variants safely
                        if (rawValue == "1" || rawValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            convertedValue = true;
                        }
                        else if (rawValue == "0" || rawValue?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            convertedValue = false;
                        }
                        else if (bool.TryParse(rawValue, out var boolVal))
                        {
                            convertedValue = boolVal;
                        }
                        else
                        {
                            throw new FormatException($"String '{rawValue}' cannot be converted to Boolean for column {col.ColumnName}.");
                        }
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        convertedValue = DateTime.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        // Ensure rawValue is not null/empty for value types before passing to ChangeType
                        if (string.IsNullOrWhiteSpace(rawValue) && targetType.IsValueType)
                        {
                            throw new InvalidCastException($"Cannot convert null or empty string to value type {targetType.Name} for column {col.ColumnName}.");
                        }
                        convertedValue = Convert.ChangeType(rawValue, targetType);
                    }

                    prop.SetValue(item, convertedValue);
                }
                catch (Exception ex)
                {
                    // Log or rethrow. Do not silently swallow without 
                    // knowing which column failed.
                    continue;
                }
            }
            yield return item;
        }
    }

    public static void ReplaceNullsWithZero(this DataTable table)
    {
        foreach (DataRow row in table.Rows)
        {
            foreach (DataColumn col in table.Columns)
            {
                if (row.IsNull(col) &&
                    (col.DataType == typeof(decimal) || col.DataType == typeof(double) ||
                     col.DataType == typeof(float) || col.DataType == typeof(int)))
                {
                    row[col] = 0m;
                }
            }
        }
    }

    public static DataTable ToDataTable<T>(this IEnumerable<T> items, string? tableName = null)
    {

        try
        {
            var dt = new DataTable(tableName ?? typeof(T).Name);
            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            foreach (var p in props)
                dt.Columns.Add(p.Name, typeof(string));

            foreach (var item in items)
            {
                var row = dt.NewRow();
                foreach (var p in props)
                {
                    var val = p.GetValue(item);
                    string? stringVal = val is null ? "" : ConvertToString(val);
                    row[p.Name] = stringVal;
                }

                dt.Rows.Add(row);

            }

            return dt;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            throw;
        }
    }


    private static string ConvertToString(object val) => val switch
    {
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
        Guid g => g.ToString(),
        bool b => b.ToString(),
        int i => i.ToString(),
        decimal d => d.ToString(),

        _ => val.ToString() ?? string.Empty,
    };
}
