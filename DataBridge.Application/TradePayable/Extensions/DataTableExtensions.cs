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
    public static IEnumerable<T> ToEnumerable<T>(this DataTable table) where T : new()
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
                if (!props.TryGetValue(col.ColumnName, out var prop) || row.IsNull(col)) continue;
                try
                {
                    var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(item, Convert.ChangeType(row[col], target));
                }
                catch { /* type mismatch between DataTable column and entity property — leave default */ }
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
                     col.DataType == typeof(float)   || col.DataType == typeof(int)))
                {
                    row[col] = 0m;
                }
            }
        }
    }

    public static DataTable ToDataTable<T>(this IEnumerable<T> items, string? tableName = null)
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
                row[p.Name] = val is null ? DBNull.Value : (object)ConvertToString(val);
            }
            dt.Rows.Add(row);
        }

        return dt;
    }

    private static string ConvertToString(object val) => val switch
    {
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
        Guid g      => g.ToString(),
        bool b      => b.ToString(),
        _           => val.ToString() ?? string.Empty,
    };
}
