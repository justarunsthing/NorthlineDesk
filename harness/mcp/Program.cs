using System.Text;
using System.Text.Json;
using Northline.Mcp;

var stdin = Console.OpenStandardInput();
var stdout = Console.OpenStandardOutput();

while (true)
{
    var body = await ReadMessageAsync(stdin);

    if (body is null)
        return 0;

    using var document = JsonDocument.Parse(body);


    var root = document.RootElement;

    if (!root.TryGetProperty("id", out var idElement))
        continue;

    var method = root.GetProperty("method").GetString();
    var response = method switch
    {
        "initialize" => Result(idElement, new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { tools = new { } },
            serverInfo = new { name = "northline-match", version = "1.0.0" },
        }),
        "tools/list" => Result(idElement, new
        {
            tools = new object[]
            {
                Tool("match_invoice", "Match INV-1001 from the received callouts. No SQL.", "invoiceNumber"),
                Tool("describe_purchase_order", "List PO-1001 callouts. Does not read the approval.", "purchaseOrderNumber"),
            },
        }),
        "tools/call" => Call(idElement, root),
        _ => Error(idElement, "Unknown method."),
    };

    await WriteMessageAsync(stdout, response);
}

static string Call(JsonElement id, JsonElement root)
{
    var arguments = new Dictionary<string, string?>(StringComparer.Ordinal);
    var name = "";

    if (root.TryGetProperty("params", out var parameters))
    {
        if (parameters.TryGetProperty("name", out var nameElement))
            name = nameElement.GetString() ?? "";

        if (parameters.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in argsElement.EnumerateObject())
                arguments[property.Name] = property.Value.ToString();
        }
    }

    var result = BoundaryTools.Call(name, arguments);

    return Result(id, new
    {
        content = new[] { new { type = "text", text = result.Text } },
        isError = result.IsError,
    });
}

static object Tool(string name, string description, string argument)
{
    return new
    {
        name,
        description,
        inputSchema = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                [argument] = new { type = "string" },
            },
        },
    };
}

static string Result(JsonElement id, object result)
{
    return JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Id(id), result });
}

static string Error(JsonElement id, string message)
{
    return JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Id(id), error = new { code = -32601, message } });
}

static object Id(JsonElement id) => id.ValueKind == JsonValueKind.Number ? id.GetInt64() : (object)(id.GetString() ?? "0");

static async Task<string?> ReadMessageAsync(Stream stdin)
{
    var header = new StringBuilder();
    var carriage = false;

    while (true)
    {
        var next = stdin.ReadByte();

        if (next < 0)
            return header.Length == 0 ? null : throw new InvalidOperationException("Incomplete MCP header.");

        var ch = (char)next;
        header.Append(ch);

        if (carriage && ch == '\n' && header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
            break;

        carriage = ch == '\r';
    }

    var length = 0;

    foreach (var line in header.ToString().Split("\r\n"))
    {
        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(line.Split(':', 2)[1].Trim(), out var parsed))
            length = parsed;
    }

    var buffer = new byte[length];
    var read = 0;

    while (read < length)
    {
        var count = await stdin.ReadAsync(buffer.AsMemory(read, length - read));

        if (count == 0)
            return null;

        read += count;
    }

    return Encoding.UTF8.GetString(buffer);
}

static async Task WriteMessageAsync(Stream stdout, string json)
{
    var bytes = Encoding.UTF8.GetBytes(json);
    var header = Encoding.ASCII.GetBytes("Content-Length: " + bytes.Length + "\r\n\r\n");

    await stdout.WriteAsync(header);
    await stdout.WriteAsync(bytes);
    await stdout.FlushAsync();
}