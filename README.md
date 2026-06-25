# OpenAPIToDoc

A C# .NET 10 console application designed to ingest an OpenAPI specification JSON file and generate a readable, less-technical Microsoft Word (`.docx`) document. 

It translates complex technical structures like nested JSON Schemas and HTTP paths into clean, easy-to-understand tables, sections, and descriptions tailored for business analysts, product managers, or clients.

---

## Walkthrough & How it Works

### 1. Key Libraries
- **`Microsoft.OpenApi.Readers`**: Parses OpenAPI 3.0/3.1 JSON specifications into a strongly-typed .NET object model.
- **`OfficeIMO.Word`**: A cross-platform, MIT-licensed wrapper around the Microsoft Open XML SDK that allows programmatically creating rich, styled Word documents without requiring Microsoft Office to be installed.

### 2. Implementation Overview
- **CLI and Interactive Loop** ([Program.cs](src/OpenAPIToDocConsole/Program.cs)): Accepts command-line arguments or defaults to an interactive mode prompting you for the OpenAPI JSON file path. It automatically writes the output to a `.docx` file in the same directory unless specified otherwise.
- **Spec Parser & Generator Core** ([OpenApiToDocGenerator.cs](src/OpenAPIToDocConsole/OpenApiToDocGenerator.cs)):
  - **Organizes by Tag**: Groups all endpoints/operations by their tags (e.g., *Customers*, *Orders*) into distinct document sections. Endpoints without tags are placed under *General Services*.
  - **Flattens Data Models**: Recursively parses complex object properties and nested structures (like arrays of objects) into flat dotted notation (e.g. `user.profile.age` or `items[].id`) displayed in clear tables.
  - **Highlights Details**: Applies colors to HTTP methods (e.g., Green for `POST`, Blue for `GET`, Orange for `PUT`, Red for `DELETE`) and prints required parameters/properties in Red bold text.

---

## How to Run

Navigate to the project directory:
```bash
cd src/OpenAPIToDocConsole
```

### Option A: Command Line Mode
Pass the input OpenAPI JSON file path and optionally the output Word document path:
```bash
dotnet run -- "/path/to/openapi.json" "/path/to/output.docx"
```
*(If the output path is omitted, the `.docx` file will be created in the same folder as the input JSON file).*

### Option B: Interactive Mode
Run without arguments and the console will prompt you to enter the path:
```bash
dotnet run
```

---

## Document Layout & Styles

The generated document contains the following sections:
1. **Title Page**: API Title, Version, Subtitle, and Contact information (name and email).
2. **Overview**: Description of the API and base servers/environments URLs.
3. **Endpoints grouped by Functional Area**:
   - **Endpoint Summary**: Large heading detailing the functional summary (e.g., *Create Customer Profile*).
   - **HTTP Details**: Displays the method and path (e.g., `[POST] /customers`).
   - **Detailed Description**: Functional business context of the endpoint.
   - **Input Parameters Table**: Lists parameters, their locations (query, path, header), types, requirement status, and descriptions.
   - **Request Body Table**: Lists flat request properties, types, and descriptions.
   - **Expected Responses Table**: Status codes (highlighted in Green for success, Red for errors), descriptions, and response data summaries.
