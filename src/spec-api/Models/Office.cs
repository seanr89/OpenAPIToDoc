namespace spec_api.Models;

public record Office(
    Guid Id,
    string Name,
    string Address,
    string Phone,
    string Email
);
