using System;
using System.Text;
using System.Text.Json.Nodes;

namespace BNetInstaller.Endpoints;

internal abstract class BaseEndpoint<T>(string endpoint, AgentClient client) where T : class, IModel, new()
{
    public string Endpoint { get; } = endpoint;
    public T Model { get; } = new();

    protected AgentClient Client { get; } = client;

    public virtual async Task<JsonNode> Get()
    {
        using var response = await Client.SendAsync(Endpoint, HttpMethod.Get);
        return await Deserialize(response);
    }

    public virtual async Task<JsonNode> Post()
    {
        if (Model is NullModel)
            return default!;

        using var response = await Client.SendAsync(Endpoint, HttpMethod.Post, Model);
        return await Deserialize(response);
    }

    public virtual async Task<JsonNode> Put()
    {
        if (Model is NullModel)
            return default!;

        using var response = await Client.SendAsync(Endpoint, HttpMethod.Put, Model);
        return await Deserialize(response);
    }

    public virtual async Task Delete()
    {
        await Client.SendAsync(Endpoint, HttpMethod.Delete);
    }

    protected async Task<JsonNode> Deserialize(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        JsonNode? result = null;
        try
        {
            result = JsonNode.Parse(content);
        }
        catch
        {
            // if bnet gives non-JSON then use the raw payload instead
            throw new Exception("Agent returned a non-JSON response.", new Exception(content));
        }

        ValidateResponse(result!, content);
        return result!;
    }

    private static int GetErrorCode(JsonNode? node)
    {
        try
        {
            var v = node?.GetValue<double?>();
            return (int)Math.Round(v.GetValueOrDefault());
        }
        catch
        {
            return 0;
        }
    }

    protected virtual void ValidateResponse(JsonNode response, string content)
    {
        var agentError = GetErrorCode(response?["error"]);

        if (agentError <= 0)
            return;

        // see if bnet agent gives an error with the form payload
        string? section = null;
        int sectionError = 0;

        if (response?["form"] is JsonObject form)
        {
            foreach (var kvp in form)
            {
                var node = kvp.Value;
                var code = GetErrorCode(node?["error"]);
                if (code > 0)
                {
                    section = kvp.Key;
                    sectionError = code;
                    break;
                }
            }
        }

        var sb = new StringBuilder();
        sb.Append($"Agent Error: {agentError} - {AgentException.Describe(agentError)}");

        if (!string.IsNullOrWhiteSpace(section))
            sb.Append($" (section: {section}, section error: {sectionError})");

        // keep raw json (debug)
        throw new AgentException(agentError, sb.ToString(), content, new Exception(content));
    }
}
