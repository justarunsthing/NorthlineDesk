namespace Northline.Mcp;

public sealed record ToolResult(bool IsError, string Text)
{
    public static ToolResult Ok(string text) => new(false, text);

    public static ToolResult Refuse(string text) => new(true, text);
}