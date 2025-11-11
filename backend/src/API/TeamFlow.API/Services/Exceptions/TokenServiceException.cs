namespace TeamFlowAPI.Services.Exceptions;

/// <summary>
/// Exception thrown when token operations fail
/// </summary>
public class TokenServiceException : Exception
{
    public TokenServiceException(string message) : base(message) { }
    public TokenServiceException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when token validation fails
/// </summary>
public class InvalidTokenException : TokenServiceException
{
    public InvalidTokenException(string message = "Invalid or expired token") : base(message) { }
}

/// <summary>
/// Exception thrown for suspicious activity detection
/// </summary>
public class SuspiciousActivityException : TokenServiceException
{
    public SuspiciousActivityException(string message = "Suspicious activity detected") : base(message) { }
}
