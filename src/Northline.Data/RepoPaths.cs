namespace Northline.Data;

public static class RepoPaths
{
    public static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NorthlineDesk.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find NorthlineDesk.sln walking up from " + AppContext.BaseDirectory);
    }

    public static string Migrations() => Path.Combine(Root(), "db", "migrations");

    public static string Explanations() => Path.Combine(Root(), "db", "explanations");
}