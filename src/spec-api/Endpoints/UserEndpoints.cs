using Microsoft.AspNetCore.Http.HttpResults;
using spec_api.Models;
using spec_api.Data;

namespace spec_api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users")
            .WithTags("Users");

        group.MapGet("/", GetUsers)
            .WithName("GetUsers")
            .WithSummary("List all users")
            .WithDescription("Retrieves a list of all users registered in the system.");

        group.MapGet("/{id:guid}", GetUser)
            .WithName("GetUserById")
            .WithSummary("Get user by ID")
            .WithDescription("Retrieves details of a specific user using their unique identifier.");

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithSummary("Create a new user")
            .WithDescription("Registers a new user in the system. Optional Office ID links the user to an office location.");

        group.MapPut("/{id:guid}", UpdateUser)
            .WithName("UpdateUser")
            .WithSummary("Update an existing user")
            .WithDescription("Updates the information of an existing user in the system.");

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .WithSummary("Delete a user")
            .WithDescription("Removes a user from the system by their unique identifier.");
    }

    private static Ok<List<User>> GetUsers()
    {
        return TypedResults.Ok(UserDataStore.GetAll());
    }

    private static Results<Ok<User>, NotFound> GetUser(Guid id)
    {
        var user = UserDataStore.GetById(id);
        return user is not null ? TypedResults.Ok(user) : TypedResults.NotFound();
    }

    private static Results<Created<User>, BadRequest<string>> CreateUser(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return TypedResults.BadRequest("First name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return TypedResults.BadRequest("Last name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return TypedResults.BadRequest("Email is required.");
        }

        // Validate OfficeId if provided
        if (request.OfficeId.HasValue && OfficeDataStore.GetById(request.OfficeId.Value) is null)
        {
            return TypedResults.BadRequest("The specified Office ID does not exist.");
        }

        var newUser = new User(
            Guid.NewGuid(),
            request.FirstName,
            request.LastName,
            request.Email,
            request.Role,
            request.OfficeId
        );

        UserDataStore.Add(newUser);
        return TypedResults.Created($"/api/users/{newUser.Id}", newUser);
    }

    private static Results<Ok<User>, BadRequest<string>, NotFound> UpdateUser(Guid id, UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return TypedResults.BadRequest("First name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return TypedResults.BadRequest("Last name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return TypedResults.BadRequest("Email is required.");
        }

        // Validate OfficeId if provided
        if (request.OfficeId.HasValue && OfficeDataStore.GetById(request.OfficeId.Value) is null)
        {
            return TypedResults.BadRequest("The specified Office ID does not exist.");
        }

        var existingUser = UserDataStore.GetById(id);
        if (existingUser is null)
        {
            return TypedResults.NotFound();
        }

        var updatedUser = existingUser with
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = request.Role,
            OfficeId = request.OfficeId
        };

        UserDataStore.Update(updatedUser);
        return TypedResults.Ok(updatedUser);
    }

    private static Results<NoContent, NotFound> DeleteUser(Guid id)
    {
        var deleted = UserDataStore.Delete(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}

public record CreateUserRequest(string FirstName, string LastName, string Email, string Role, Guid? OfficeId);
public record UpdateUserRequest(string FirstName, string LastName, string Email, string Role, Guid? OfficeId);
