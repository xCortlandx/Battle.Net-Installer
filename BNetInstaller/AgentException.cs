using System;

namespace BNetInstaller;

internal sealed class AgentException : Exception
{
    public int ErrorCode { get; }
    public string? ResponseContent { get; }

    public AgentException(int errorCode, string message, string? responseContent = null, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        ResponseContent = responseContent;
    }

    public static string Describe(int errorCode) => errorCode switch
    {
        2221 => "The supplied TACT Product is unavailable or invalid.",
        2421 => "Your computer doesn't meet the minimum specs and/or space requirements.",
        3001 => "The supplied TACT Product requires an encryption key which is missing.",
        _ => "The Battle.net Agent returned an error."
    };
}
