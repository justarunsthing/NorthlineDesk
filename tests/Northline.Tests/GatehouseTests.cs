using Northline.Data;
using Northline.Gatehouse;

namespace Northline.Tests;

public sealed class GatehouseTests
{
    [Fact]
    public void Source_passes_the_gate()
    {
        var findings = Scanner.Scan(RepoPaths.Root(), fixtures: false);

        Assert.Empty(findings);
    }

    [Fact]
    public void Fixtures_are_rejected_for_the_three_rules()
    {
        var findings = Scanner.Scan(RepoPaths.Root(), fixtures: true);
        var rules = findings.Select(finding => finding.Rule).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("shallow-test", rules);
        Assert.Contains("package", rules);
        Assert.Contains("drop", rules);
        Assert.Contains("hidden-table", rules);
    }
}