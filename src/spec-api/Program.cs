using spec_api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Spec API",
            Version = "v1.0.0",
            Description = "A base API service providing endpoints for offices and users, designed to demonstrate document generation capabilities.",
            Contact = new Microsoft.OpenApi.OpenApiContact
            {
                Name = "API Developer",
                Email = "dev@example.com",
                Url = new Uri("https://github.com/seanr89/OpenAPIToDoc")
            },
            License = new Microsoft.OpenApi.OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapOfficeEndpoints();
app.MapUserEndpoints();

app.Run();
