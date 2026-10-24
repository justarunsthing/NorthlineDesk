namespace Northline.Gatehouse;

public static class Scanner
{
    private static readonly string[] StrongAssertions =
    [
        "Assert.Equal",
        "Assert.NotEqual",
        "Assert.True",
        "Assert.False",
        "Assert.Contains",
        "Assert.DoesNotContain",
        "Assert.Null",
        "Assert.Empty",
        "Assert.Throws",
        "Assert.StartsWith",
        "Assert.EndsWith",
        "Assert.InRange",
        "Assert.Fail",
        "Assert.Matches",
    ];

    private static readonly string[] AgentRoots =
    [
        "src/Northline.Match",
        "src/Northline.Data",
        "src/Northline.MatchApi",
        "tools/Northline.Migrator",
        "harness/mcp",
    ];

    private static readonly string[] HiddenTables = ["dbo.Approval", "dbo.Requisition", "dbo.VatRate"];

    public static IReadOnlyList<Finding> Scan(string repoRoot, bool fixtures)
    {
        var findings = new List<Finding>();
        var allowlist = ReadAllowlist(Path.Combine(repoRoot, "harness", "package-allowlist.txt"));

        if (fixtures)
        {
            var fixtureRoot = Path.Combine(repoRoot, "gate", "fixtures");

            ScanTree(fixtureRoot, fixtureRoot, findings, allowlist, sqlRules: true, shallow: true, packages: true, hiddenTables: true);

            return findings;
        }

        foreach (var relative in AgentRoots)
        {
            var root = Path.Combine(repoRoot, relative);

            if (Directory.Exists(root))
                ScanTree(root, repoRoot, findings, allowlist, sqlRules: false, shallow: false, packages: false, hiddenTables: true);
        }

        ScanTree(Path.Combine(repoRoot, "tests"), repoRoot, findings, allowlist, sqlRules: false, shallow: true, packages: true, hiddenTables: false);
        ScanTree(Path.Combine(repoRoot, "src"), repoRoot, findings, allowlist, sqlRules: false, shallow: true, packages: true, hiddenTables: false);
        ScanTree(Path.Combine(repoRoot, "tools"), repoRoot, findings, allowlist, sqlRules: false, shallow: true, packages: true, hiddenTables: false);
        ScanSql(Path.Combine(repoRoot, "db"), repoRoot, findings);

        return findings;
    }

    private static void ScanTree(
        string root,
        string repoRoot,
        List<Finding> findings,
        HashSet<string> allowlist,
        bool sqlRules,
        bool shallow,
        bool packages,
        bool hiddenTables)
    {
        if (!Directory.Exists(root))
            return;

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var relative = Path.GetRelativePath(repoRoot, file);
            var normalized = relative.Replace('\\', '/');

            if (normalized.StartsWith("gate/", StringComparison.Ordinal) && !sqlRules)
                continue;

            var isGatehouse = normalized.Contains("tools/Northline.Gatehouse/", StringComparison.Ordinal);

            if (shallow && !isGatehouse && file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                ScanShallow(file, relative, findings);

            if (packages && file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                ScanPackages(file, relative, allowlist, findings);

            if (hiddenTables && (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)))
                ScanHiddenTables(file, relative, findings);

            if (sqlRules && file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                ScanSqlFile(file, relative, findings, estateSeedAllowed: false);
        }
    }

    private static void ScanSql(string dbRoot, string repoRoot, List<Finding> findings)
    {
        if (!Directory.Exists(dbRoot))
            return;

        foreach (var file in Directory.EnumerateFiles(dbRoot, "*.sql", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(repoRoot, file);
            var estateSeed = relative.Replace('\\', '/').EndsWith("db/migrations/001_estate.sql", StringComparison.Ordinal);

            ScanSqlFile(file, relative, findings, estateSeed);
        }
    }

    private static void ScanShallow(string file, string relative, List<Finding> findings)
    {
        var lines = File.ReadAllLines(file);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("[Fact]", StringComparison.Ordinal) && !lines[i].Contains("[Theory]", StringComparison.Ordinal))
                continue;

            var end = lines.Length;

            for (var j = i + 1; j < lines.Length; j++)
            {
                if (lines[j].Contains("[Fact]", StringComparison.Ordinal) || lines[j].Contains("[Theory]", StringComparison.Ordinal))
                {
                    end = j;
                    break;
                }
            }

            var body = string.Join('\n', lines.Skip(i).Take(end - i));
            var shallow = body.Contains("Assert.NotNull", StringComparison.Ordinal)
                || body.Contains("Assert.IsNotNull", StringComparison.Ordinal);
            var strong = StrongAssertions.Any(assertion => body.Contains(assertion, StringComparison.Ordinal));

            if (shallow && !strong)
            {
                findings.Add(new Finding(relative, i + 1, "shallow-test", "The only assertion is not-null."));
            }
        }
    }

    private static void ScanPackages(string file, string relative, HashSet<string> allowlist, List<Finding> findings)
    {
        var lines = File.ReadAllLines(file);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            const string marker = "PackageReference Include=\"";
            var start = line.IndexOf(marker, StringComparison.Ordinal);

            if (start < 0)
                continue;

            start += marker.Length;
            var end = line.IndexOf('"', start);

            if (end < 0)
                continue;

            var name = line[start..end];

            if (!allowlist.Contains(name))
                findings.Add(new Finding(relative, i + 1, "package", name + " is not on the allow list."));
        }
    }

    private static void ScanHiddenTables(string file, string relative, List<Finding> findings)
    {
        var lines = File.ReadAllLines(file);

        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var table in HiddenTables)
            {
                if (lines[i].Contains(table, StringComparison.Ordinal))
                    findings.Add(new Finding(relative, i + 1, "hidden-table", table + " is outside the agent boundary."));
            }
        }
    }

    private static void ScanSqlFile(string file, string relative, List<Finding> findings, bool estateSeedAllowed)
    {
        var lines = File.ReadAllLines(file);
        var retirement = Path.GetFileName(file).StartsWith("002_", StringComparison.Ordinal);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (!retirement && ContainsDrop(line))
                findings.Add(new Finding(relative, i + 1, "drop", "A drop belongs in migration 002."));

            if (IsHiddenWrite(line) && !estateSeedAllowed)
                findings.Add(new Finding(relative, i + 1, "hidden-table", "A write to an approval, requisition, or VAT table is outside the agent boundary."));
        }
    }

    private static bool ContainsDrop(string line)
    {
        return line.Contains("DROP PROCEDURE", StringComparison.OrdinalIgnoreCase)
            || line.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase)
            || line.Contains("DROP VIEW", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHiddenWrite(string line)
    {
        var upper = line.ToUpperInvariant();
        var writes = upper.Contains("UPDATE ", StringComparison.Ordinal)
            || upper.Contains("DELETE ", StringComparison.Ordinal)
            || upper.Contains("INSERT ", StringComparison.Ordinal);

        if (!writes)
            return false;

        return line.Contains("dbo.Approval", StringComparison.OrdinalIgnoreCase)
            || line.Contains("dbo.Requisition", StringComparison.OrdinalIgnoreCase)
            || line.Contains("dbo.VatRate", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> ReadAllowlist(string path)
    {
        if (!File.Exists(path))
            return new HashSet<string>(StringComparer.Ordinal);

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);
    }
}