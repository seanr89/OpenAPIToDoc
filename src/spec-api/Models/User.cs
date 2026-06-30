namespace spec_api.Models;

public record User(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    Guid? OfficeId
);
