namespace Users.Infrastructure.Implementation.Identity.Keycloak;

internal record UserRepresentation(
    string Username,
    string Email,
    bool EmailVerified,
    bool Enabled,
    CredentialRepresentation[] Credentials);
    
internal record CredentialRepresentation(string Type, string Value, bool Temporary);