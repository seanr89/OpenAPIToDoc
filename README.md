# OpenAPIToDoc

A C# .NET 10 console application designed to ingest an OpenAPI specification JSON file and generate a readable, less-technical Microsoft Word (`.docx`) document. 

It translates complex technical structures like nested JSON Schemas and HTTP paths into clean, easy-to-understand tables, sections, and descriptions tailored for business analysts, product managers, or clients.

---

## Walkthrough & How it Works

### 1. Key Projects
- **`OpenAPIToDocConsole`**: The generator CLI application.
- **`spec-api`**: A reference ASP.NET Core web API that serves as the base specification project. It uses native .NET 10 OpenAPI generation (`Microsoft.AspNetCore.OpenApi`).

### 2. Key Libraries
- **`Microsoft.OpenApi` & `Microsoft.OpenApi.Readers`**: Parses OpenAPI 3.0/3.1 JSON specifications into a strongly-typed .NET object model.
- **`OfficeIMO.Word`**: A cross-platform, MIT-licensed wrapper around the Microsoft Open XML SDK that allows programmatically creating rich, styled Word documents without requiring Microsoft Office to be installed.

### 3. Implementation Overview
- **CLI and Interactive Loop** ([Program.cs](src/OpenAPIToDocConsole/Program.cs)): Accepts command-line arguments or defaults to an interactive mode prompting you for the OpenAPI JSON file path. By default, it names and saves the output `.docx` file using the API specification's title and version (e.g., `<Title>_<Version>.docx`).
- **Spec Parser & Generator Core** ([OpenApiToDocGenerator.cs](src/OpenAPIToDocConsole/OpenApiToDocGenerator.cs)):
  - **Dynamic Naming & Sanitization**: Extracts the API `Title` and `Version` from the parsed spec to generate the default output filename, sanitizing any characters that are invalid for file paths.
  - **Organizes by Tag**: Groups all endpoints/operations by their tags (e.g., *Customers*, *Orders*) into distinct document sections. Endpoints without tags are placed under *General Services*.
  - **Flattens Data Models**: Recursively parses complex object properties and nested structures (like arrays of objects) into flat dotted notation (e.g. `user.profile.age` or `items[].id`) displayed in clear tables.
  - **Highlights Details**: Applies colors to HTTP methods (e.g., Green for `POST`, Blue for `GET`, Orange for `PUT`, Red for `DELETE`) and prints required parameters/properties in Red bold text.

---

## The Base spec-api Project

The project includes a sample API implementation under `src/spec-api` configured with native OpenAPI specification generation. In [Program.cs](src/spec-api/Program.cs), it registers OpenAPI document customization via an options-based document transformer:

```csharp
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
```

Building the `spec-api` project automatically regenerates the `openapi.json` file in the project folder.

---

## How to Run

This repository consists of two main projects: the reference API (`spec-api`) and the generator console application (`OpenAPIToDocConsole`).

### 1. Reference Web API (`spec-api`)

The `spec-api` project is a sample ASP.NET Core application configured with native .NET OpenAPI document generation.

#### Run the Web API locally:
Navigate to the `spec-api` project directory and run it:
```bash
cd src/spec-api
dotnet run
```
Once started, the API will be available at:
* **HTTP**: `http://localhost:5016`
* **HTTPS**: `https://localhost:7110`

When running in the `Development` environment, you can access the dynamically generated OpenAPI specification document in your browser at:
* `http://localhost:5016/openapi/v1.json`

#### Using the Makefile:
For convenience, a `Makefile` is provided in the `src/spec-api` directory:
- **Build the API**:
  ```bash
  make build
  ```
- **Generate `openapi.json` without running the application**:
  ```bash
  make openapi
  ```
  *(This compiles the app in Release configuration and extracts the OpenAPI JSON document directly using `Microsoft.Extensions.ApiDescription.Server` into `src/spec-api/openapi.json`)*
- **Clean build artifacts**:
  ```bash
  make clean
  ```

---

### 2. Generator CLI (`OpenAPIToDocConsole`)

The generator CLI parses an OpenAPI JSON file and outputs a formatted Word document (`.docx`).

Navigate to the generator CLI directory:
```bash
cd src/OpenAPIToDocConsole
```

#### Option A: Command Line Mode
Pass the input OpenAPI JSON file path and optionally the output Word document path or directory:
```bash
dotnet run -- "/path/to/openapi.json" "/path/to/output_or_directory"
```
* **If the output path is omitted**: The `.docx` file will be created in the same folder as the input JSON file and named after the spec's title and version (e.g., `Spec API_v1.0.0.docx`).
* **If the output path is a directory**: The `.docx` file is saved inside that directory using the `<Title>_<Version>.docx` naming convention.
* **If the output path is a file path**: The file is created exactly at that path.

*Example (running against the generated spec-api JSON)*:
```bash
dotnet run -- "../spec-api/openapi.json"
```

#### Option B: Interactive Mode
Run without arguments, and the console will prompt you to enter the path to your OpenAPI JSON file:
```bash
dotnet run
```

---

## Document Layout & Styles

The generated document contains the following sections:
1. **Title Page**: API Title, Version, Subtitle, and Contact information (name, email, and URL).
2. **Overview**: Description of the API and base servers/environments URLs.
3. **Endpoints grouped by Functional Area**:
   - **Endpoint Summary**: Large heading detailing the functional summary (e.g., *Create Customer Profile*).
   - **HTTP Details**: Displays the method and path (e.g., `[POST] /customers`).
   - **Detailed Description**: Functional business context of the endpoint.
   - **Input Parameters Table**: Lists parameters, their locations (query, path, header), types, requirement status, and descriptions.
   - **Request Body Table**: Lists flat request properties, types, and descriptions.
   - **Expected Responses Table**: Status codes (highlighted in Green for success, Red for errors), descriptions, and response data summaries.
