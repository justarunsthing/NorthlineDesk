namespace Northline.Gatehouse;

public sealed record Finding(string Path, int Line, string Rule, string Detail);