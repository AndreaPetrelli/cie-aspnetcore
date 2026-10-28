using System;

namespace CIE.AspNetCore.Authentication.Exceptions;

/// <summary>An authentication failure reported by the CIE identity provider.</summary>
public sealed class CieException : Exception
{
    public int ErrorCode { get; }

    public CieException(int errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
