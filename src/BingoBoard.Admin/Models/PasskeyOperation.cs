namespace BingoBoard.Admin.Models;

/// <summary>
/// Represents an operation to obtain a passkey credential.
/// </summary>
public enum PasskeyOperation
{
    /// <summary>
    /// Create a new passkey.
    /// </summary>
    /// <remarks>
    /// Used during registration.
    /// </remarks>
    Create = 0,

    /// <summary>
    /// Request an existing passkey.
    /// </summary>
    /// <remarks>
    /// Used during authentication.
    /// </remarks>
    Request = 1,
}
