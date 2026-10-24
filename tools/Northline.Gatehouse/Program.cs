using Northline.Gatehouse;

var fixtures = args.Contains("--fixtures", StringComparer.Ordinal);
var root = FindRoot();
var findings = Scanner.Scan(root, fixtures);

foreach (var finding in findings)
    Console.Error.WriteLine($"{finding.Rule}: {finding.Path}:{finding.Line} {finding.Detail}");

if (findings.Count == 0)
{
    Console.WriteLine(fixtures
        ? "Fixture scan found nothing. The red-gate demo has nothing to reject."
        : "Gatehouse found nothing.");
    return fixtures ? 1 : 0;
}

Console.Error.WriteLine(findings.Count + " finding(s).");

return 1;

static string FindRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);

    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "NorthlineDesk.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    dir = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "NorthlineDesk.sln")))
            return dir.FullName;

        dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not find NorthlineDesk.sln.");
}