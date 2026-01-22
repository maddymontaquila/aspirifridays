namespace BingoBoard.Admin.Models;

/// <summary>
/// Represents a passkey submitted by a user.
/// </summary>
public class SubmittedPasskey
{
    /// <summary>
    /// Gets or sets the JSON representation of the passkey credential.
    /// </summary>
    public string? CredentialJson { get; set; }

    /// <summary>
    /// Gets or sets the error that occurred while obtaining the credential.
    /// </summary>
    public string? Error { get; set; }
}
