using System.Text;

namespace Northline.Data;

public static class SqlBatches
{
    public static IReadOnlyList<string> Split(string sql)
    {
        var batches = new List<string>();
        var current = new StringBuilder();

        foreach (var raw in sql.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (raw.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                Add(batches, current);
                current.Clear();
                continue;
            }

            current.AppendLine(raw);
        }

        Add(batches, current);

        return batches;
    }

    private static void Add(List<string> batches, StringBuilder current)
    {
        var text = current.ToString().Trim();

        if (text.Length > 0)
            batches.Add(text);
    }
}