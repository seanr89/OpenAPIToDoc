using Microsoft.AspNetCore.Http.HttpResults;
using spec_api.Models;
using spec_api.Data;

namespace spec_api.Endpoints;

public static class OfficeEndpoints
{
    public static void MapOfficeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/offices")
            .WithTags("Offices");

        group.MapGet("/", GetOffices)
            .WithName("GetOffices")
            .WithSummary("List all offices")
            .WithDescription("Retrieves a list of all office locations registered in the system.");

        group.MapGet("/{id:guid}", GetOffice)
            .WithName("GetOfficeById")
            .WithSummary("Get office by ID")
            .WithDescription("Retrieves details of a specific office location using its unique identifier.");

        group.MapPost("/", CreateOffice)
            .WithName("CreateOffice")
            .WithSummary("Create a new office")
            .WithDescription("Registers a new office location in the database with the provided details.");

        group.MapPut("/{id:guid}", UpdateOffice)
            .WithName("UpdateOffice")
            .WithSummary("Update an existing office")
            .WithDescription("Updates the information of an existing office location.");

        group.MapDelete("/{id:guid}", DeleteOffice)
            .WithName("DeleteOffice")
            .WithSummary("Delete an office")
            .WithDescription("Removes an office location from the system by its unique identifier.");
    }

    private static Ok<List<Office>> GetOffices()
    {
        return TypedResults.Ok(OfficeDataStore.GetAll());
    }

    private static Results<Ok<Office>, NotFound> GetOffice(Guid id)
    {
        var office = OfficeDataStore.GetById(id);
        return office is not null ? TypedResults.Ok(office) : TypedResults.NotFound();
    }

    private static Results<Created<Office>, BadRequest<string>> CreateOffice(CreateOfficeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Office name is required.");
        }

        var newOffice = new Office(
            Guid.NewGuid(),
            request.Name,
            request.Address,
            request.Phone,
            request.Email
        );

        OfficeDataStore.Add(newOffice);
        return TypedResults.Created($"/api/offices/{newOffice.Id}", newOffice);
    }

    private static Results<Ok<Office>, BadRequest<string>, NotFound> UpdateOffice(Guid id, UpdateOfficeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Office name is required.");
        }

        var existingOffice = OfficeDataStore.GetById(id);
        if (existingOffice is null)
        {
            return TypedResults.NotFound();
        }

        var updatedOffice = existingOffice with
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email
        };

        OfficeDataStore.Update(updatedOffice);
        return TypedResults.Ok(updatedOffice);
    }

    private static Results<NoContent, NotFound> DeleteOffice(Guid id)
    {
        var deleted = OfficeDataStore.Delete(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}

public record CreateOfficeRequest(string Name, string Address, string Phone, string Email);
public record UpdateOfficeRequest(string Name, string Address, string Phone, string Email);
