using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using Microsoft.OpenApi.Readers;
using OfficeIMO.Word;
using OfficeIMO.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OpenAPIToDocConsole
{
    public static class OpenApiToDocGenerator
    {
        private class FlatProperty
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public bool Required { get; set; }
            public string Description { get; set; } = "";
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        public static string GenerateWordDoc(string openApiJsonPath, string? outputWordPath = null)
        {
            if (!File.Exists(openApiJsonPath))
            {
                throw new FileNotFoundException("The specified OpenAPI JSON file could not be found.", openApiJsonPath);
            }

            Console.WriteLine($"Reading and parsing OpenAPI specification from: {openApiJsonPath}");
            OpenApiDocument? openApiDoc = null;
            using (var stream = File.OpenRead(openApiJsonPath))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Position = 0;
                var readResult = OpenApiDocument.Load(ms);
                openApiDoc = readResult.Document;
                var diagnostic = readResult.Diagnostic;
                if (diagnostic != null && diagnostic.Errors.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Warning: The following OpenAPI specification parsing diagnostics were reported:");
                    foreach (var error in diagnostic.Errors)
                    {
                        Console.WriteLine($"- {error.Message} (at {error.Pointer})");
                    }
                    Console.ResetColor();
                }
            }

            if (openApiDoc == null)
            {
                throw new InvalidDataException("Failed to parse the OpenAPI document. Ensure the file is a valid OpenAPI JSON spec.");
            }

            string actualOutputPath;
            string title = openApiDoc.Info?.Title ?? "API_Specification";
            string version = openApiDoc.Info?.Version ?? "1.0.0";
            string defaultFileName = SanitizeFileName($"{title}_{version}.docx");

            if (string.IsNullOrEmpty(outputWordPath))
            {
                string directory = Path.GetDirectoryName(openApiJsonPath) ?? "";
                actualOutputPath = Path.Combine(directory, defaultFileName);
            }
            else if (Directory.Exists(outputWordPath))
            {
                actualOutputPath = Path.Combine(outputWordPath, defaultFileName);
            }
            else
            {
                actualOutputPath = outputWordPath;
            }

            Console.WriteLine($"Generating Word document: {actualOutputPath}");
            using (var document = WordDocument.Create(actualOutputPath))
            {
                // Document Header/Title Page info
                var titlePara = document.AddParagraph(openApiDoc.Info?.Title ?? "API Specification");
                titlePara.SetBold();
                titlePara.SetFontSize(28);

                var subtitlePara = document.AddParagraph($"Version {openApiDoc.Info?.Version ?? "1.0.0"}");
                subtitlePara.SetFontSize(14);
                subtitlePara.SetItalic();

                document.AddParagraph(); // Spacer

                if (openApiDoc.Info != null && !string.IsNullOrEmpty(openApiDoc.Info.Description))
                {
                    var descHeader = document.AddParagraph("Overview");
                    descHeader.Style = WordParagraphStyles.Heading1;
                    descHeader.SetBold();
                    document.AddParagraph(openApiDoc.Info.Description);
                    document.AddParagraph();
                }

                // Contact Details
                if (openApiDoc.Info?.Contact != null && 
                    (!string.IsNullOrEmpty(openApiDoc.Info.Contact.Name) || !string.IsNullOrEmpty(openApiDoc.Info.Contact.Email)))
                {
                    var contactHeader = document.AddParagraph("Contact Information");
                    contactHeader.Style = WordParagraphStyles.Heading2;
                    contactHeader.SetBold();
                    if (!string.IsNullOrEmpty(openApiDoc.Info.Contact.Name))
                        document.AddParagraph($"Name: {openApiDoc.Info.Contact.Name}");
                    if (!string.IsNullOrEmpty(openApiDoc.Info.Contact.Email))
                        document.AddParagraph($"Email: {openApiDoc.Info.Contact.Email}");
                    document.AddParagraph();
                }

                // Server Base URLs
                if (openApiDoc.Servers != null && openApiDoc.Servers.Any())
                {
                    var serversHeader = document.AddParagraph("Base Server URLs");
                    serversHeader.Style = WordParagraphStyles.Heading2;
                    serversHeader.SetBold();
                    foreach (var server in openApiDoc.Servers)
                    {
                        var urlPara = document.AddParagraph($"- {server.Url}");
                        if (!string.IsNullOrEmpty(server.Description))
                        {
                            urlPara.AddText($" ({server.Description})").SetItalic();
                        }
                    }
                    document.AddParagraph();
                }

                // Page break before detailed endpoints
                document.AddPageBreak();

                // Group endpoints by Tags
                var tagGroups = new Dictionary<string, List<(string Path, HttpMethod Method, OpenApiOperation Operation)>>();

                if (openApiDoc.Paths != null)
                {
                    foreach (var path in openApiDoc.Paths)
                    {
                        if (path.Value?.Operations == null) continue;

                        foreach (var operation in path.Value.Operations)
                        {
                            var tags = operation.Value.Tags;
                            if (tags != null && tags.Any())
                            {
                                foreach (var tag in tags)
                                {
                                    if (string.IsNullOrEmpty(tag?.Name)) continue;
                                    if (!tagGroups.ContainsKey(tag.Name))
                                    {
                                        tagGroups[tag.Name] = new List<(string, HttpMethod, OpenApiOperation)>();
                                    }
                                    tagGroups[tag.Name].Add((path.Key, operation.Key, operation.Value));
                                }
                            }
                            else
                            {
                                const string noTagKey = "General Services";
                                if (!tagGroups.ContainsKey(noTagKey))
                                {
                                    tagGroups[noTagKey] = new List<(string, HttpMethod, OpenApiOperation)>();
                                }
                                tagGroups[noTagKey].Add((path.Key, operation.Key, operation.Value));
                            }
                        }
                    }
                }

                if (tagGroups.Count == 0)
                {
                    var emptyPara = document.AddParagraph("No API endpoints or operations found in the specification.");
                    emptyPara.SetItalic();
                }
                else
                {
                    var tocHeader = document.AddParagraph("Detailed Functional Endpoints");
                    tocHeader.Style = WordParagraphStyles.Heading1;
                    tocHeader.SetBold();
                    document.AddParagraph();

                    foreach (var group in tagGroups.OrderBy(g => g.Key))
                    {
                        var tagName = group.Key;
                        var operations = group.Value;

                        // Section for Tag
                        var groupHeader = document.AddParagraph(tagName);
                        groupHeader.Style = WordParagraphStyles.Heading1;
                        groupHeader.SetBold();

                        var tagObj = openApiDoc.Tags?.FirstOrDefault(t => string.Equals(t?.Name, tagName, StringComparison.OrdinalIgnoreCase));
                        if (tagObj != null && !string.IsNullOrEmpty(tagObj.Description))
                        {
                            document.AddParagraph(tagObj.Description).SetItalic();
                        }

                        document.AddParagraph(); // spacing

                        foreach (var opInfo in operations.OrderBy(o => o.Path).ThenBy(o => o.Method.Method))
                        {
                            var path = opInfo.Path;
                            var method = opInfo.Method;
                            var op = opInfo.Operation;

                            // Title of operation
                            string headingText = !string.IsNullOrEmpty(op.Summary) ? op.Summary : $"{method.ToString().ToUpper()} {path}";
                            var opHeader = document.AddParagraph(headingText);
                            opHeader.Style = WordParagraphStyles.Heading2;
                            opHeader.SetBold();

                            // Path and method info
                            var pathPara = document.AddParagraph();
                            var methodRun = pathPara.AddText($"[{method.ToString().ToUpper()}] ");
                            methodRun.SetBold();
                            methodRun.Color = GetMethodColor(method);
                            var pathRun = pathPara.AddText(path);
                            pathRun.SetBold();

                            // Description
                            if (!string.IsNullOrEmpty(op.Description))
                            {
                                document.AddParagraph(op.Description);
                            }
                            else if (string.IsNullOrEmpty(op.Summary))
                            {
                                document.AddParagraph("No description provided for this endpoint.");
                            }
                            document.AddParagraph();

                            // Parameters Table
                            var allParams = op.Parameters;
                            if (allParams != null && allParams.Any())
                            {
                                var subH = document.AddParagraph("Input Parameters");
                                subH.Style = WordParagraphStyles.Heading3;
                                subH.SetBold();

                                var paramTable = document.AddTable(allParams.Count + 1, 5, WordTableStyle.GridTable4Accent1);

                                // Headers
                                paramTable.Rows[0].Cells[0].Paragraphs[0].Text = "Parameter";
                                paramTable.Rows[0].Cells[0].Paragraphs[0].SetBold();
                                paramTable.Rows[0].Cells[1].Paragraphs[0].Text = "Location";
                                paramTable.Rows[0].Cells[1].Paragraphs[0].SetBold();
                                paramTable.Rows[0].Cells[2].Paragraphs[0].Text = "Type";
                                paramTable.Rows[0].Cells[2].Paragraphs[0].SetBold();
                                paramTable.Rows[0].Cells[3].Paragraphs[0].Text = "Required";
                                paramTable.Rows[0].Cells[3].Paragraphs[0].SetBold();
                                paramTable.Rows[0].Cells[4].Paragraphs[0].Text = "Description";
                                paramTable.Rows[0].Cells[4].Paragraphs[0].SetBold();

                                for (int i = 0; i < allParams.Count; i++)
                                {
                                    var param = allParams[i];
                                    var row = paramTable.Rows[i + 1];

                                    row.Cells[0].Paragraphs[0].Text = param.Name ?? "";
                                    row.Cells[0].Paragraphs[0].SetBold();

                                    row.Cells[1].Paragraphs[0].Text = param.In?.ToString() ?? "Query";
                                    row.Cells[2].Paragraphs[0].Text = GetSchemaTypeString(param.Schema);
                                    
                                    row.Cells[3].Paragraphs[0].Text = param.Required ? "Yes" : "No";
                                    if (param.Required)
                                    {
                                        row.Cells[3].Paragraphs[0].SetBold();
                                        row.Cells[3].Paragraphs[0].Color = OfficeColor.Red;
                                    }

                                    row.Cells[4].Paragraphs[0].Text = param.Description ?? "";
                                }
                                document.AddParagraph();
                            }

                            // Request Body Table
                            if (op.RequestBody != null)
                            {
                                var subH = document.AddParagraph("Request Body");
                                subH.Style = WordParagraphStyles.Heading3;
                                subH.SetBold();

                                if (!string.IsNullOrEmpty(op.RequestBody.Description))
                                {
                                    document.AddParagraph(op.RequestBody.Description);
                                }

                                var content = op.RequestBody.Content;
                                if (content != null && content.Any())
                                {
                                    var firstContent = content.FirstOrDefault();
                                    document.AddParagraph($"Format: {firstContent.Key}").SetItalic();

                                    var schema = firstContent.Value?.Schema;
                                    if (schema != null)
                                    {
                                        var propsList = FlattenProperties(schema);
                                        if (propsList.Any())
                                        {
                                            var bodyTable = document.AddTable(propsList.Count + 1, 4, WordTableStyle.GridTable4Accent1);

                                            bodyTable.Rows[0].Cells[0].Paragraphs[0].Text = "Property";
                                            bodyTable.Rows[0].Cells[0].Paragraphs[0].SetBold();
                                            bodyTable.Rows[0].Cells[1].Paragraphs[0].Text = "Type";
                                            bodyTable.Rows[0].Cells[1].Paragraphs[0].SetBold();
                                            bodyTable.Rows[0].Cells[2].Paragraphs[0].Text = "Required";
                                            bodyTable.Rows[0].Cells[2].Paragraphs[0].SetBold();
                                            bodyTable.Rows[0].Cells[3].Paragraphs[0].Text = "Description";
                                            bodyTable.Rows[0].Cells[3].Paragraphs[0].SetBold();

                                            for (int i = 0; i < propsList.Count; i++)
                                            {
                                                var prop = propsList[i];
                                                var row = bodyTable.Rows[i + 1];

                                                row.Cells[0].Paragraphs[0].Text = prop.Name;
                                                row.Cells[0].Paragraphs[0].SetBold();

                                                row.Cells[1].Paragraphs[0].Text = prop.Type;
                                                row.Cells[2].Paragraphs[0].Text = prop.Required ? "Yes" : "No";
                                                if (prop.Required)
                                                {
                                                    row.Cells[2].Paragraphs[0].SetBold();
                                                    row.Cells[2].Paragraphs[0].Color = OfficeColor.Red;
                                                }
                                                row.Cells[3].Paragraphs[0].Text = prop.Description;
                                            }
                                        }
                                        else
                                        {
                                            document.AddParagraph($"Expected Data Type: {GetSchemaTypeString(schema)}");
                                        }
                                    }
                                }
                                document.AddParagraph();
                            }

                            // Responses Table
                            if (op.Responses != null && op.Responses.Any())
                            {
                                var subH = document.AddParagraph("Responses");
                                subH.Style = WordParagraphStyles.Heading3;
                                subH.SetBold();

                                var responseTable = document.AddTable(op.Responses.Count + 1, 3, WordTableStyle.GridTable4Accent1);

                                responseTable.Rows[0].Cells[0].Paragraphs[0].Text = "Status Code";
                                responseTable.Rows[0].Cells[0].Paragraphs[0].SetBold();
                                responseTable.Rows[0].Cells[1].Paragraphs[0].Text = "Description";
                                responseTable.Rows[0].Cells[1].Paragraphs[0].SetBold();
                                responseTable.Rows[0].Cells[2].Paragraphs[0].Text = "Response Data";
                                responseTable.Rows[0].Cells[2].Paragraphs[0].SetBold();

                                int idx = 0;
                                foreach (var resp in op.Responses.OrderBy(r => r.Key))
                                {
                                    var row = responseTable.Rows[idx + 1];
                                    
                                    row.Cells[0].Paragraphs[0].Text = resp.Key;
                                    row.Cells[0].Paragraphs[0].SetBold();

                                    if (resp.Key.StartsWith("2"))
                                    {
                                        row.Cells[0].Paragraphs[0].Color = OfficeColor.Green;
                                    }
                                    else if (resp.Key.StartsWith("4") || resp.Key.StartsWith("5"))
                                    {
                                        row.Cells[0].Paragraphs[0].Color = OfficeColor.Red;
                                    }

                                    row.Cells[1].Paragraphs[0].Text = resp.Value?.Description ?? "";

                                    var responseContent = resp.Value?.Content;
                                    if (responseContent != null && responseContent.Any())
                                    {
                                        var firstRespContent = responseContent.FirstOrDefault();
                                        var respSchema = firstRespContent.Value?.Schema;
                                        if (respSchema != null)
                                        {
                                            row.Cells[2].Paragraphs[0].Text = GetSchemaTypeString(respSchema);
                                        }
                                        else
                                        {
                                            row.Cells[2].Paragraphs[0].Text = "None";
                                        }
                                    }
                                    else
                                    {
                                        row.Cells[2].Paragraphs[0].Text = "None";
                                    }
                                    idx++;
                                }
                                document.AddParagraph();
                            }

                            // Divider
                            var divider = document.AddParagraph("________________________________________________");
                            divider.ParagraphAlignment = JustificationValues.Center;
                            divider.Color = OfficeColor.LightGray;
                            document.AddParagraph();
                        }
                    }
                }

                document.Save();
            }
            Console.WriteLine("Word document successfully created!");
            return actualOutputPath;
        }

        private static OfficeColor GetMethodColor(HttpMethod method)
        {
            var m = method.Method.ToUpperInvariant();
            switch (m)
            {
                case "GET":
                    return OfficeColor.Blue;
                case "POST":
                    return OfficeColor.Green;
                case "PUT":
                    return OfficeColor.Orange;
                case "DELETE":
                    return OfficeColor.Red;
                default:
                    return OfficeColor.Black;
            }
        }

        private static string GetSchemaTypeString(IOpenApiSchema? schema)
        {
            if (schema == null) return "any";

            if (schema is OpenApiSchemaReference schemaReference && schemaReference.Reference != null)
            {
                return schemaReference.Reference.Id ?? "any";
            }

            if (schema.Type == JsonSchemaType.Array)
            {
                var itemType = GetSchemaTypeString(schema.Items);
                return $"List of {itemType}";
            }

            if (!string.IsNullOrEmpty(schema.Format))
            {
                return $"{schema.Type?.ToString().ToLowerInvariant()} ({schema.Format})";
            }

            return schema.Type?.ToString().ToLowerInvariant() ?? "object";
        }

        private static List<FlatProperty> FlattenProperties(IOpenApiSchema schema, string prefix = "", int depth = 0, HashSet<IOpenApiSchema>? visited = null)
        {
            var list = new List<FlatProperty>();
            if (schema == null || depth > 3) return list;

            visited ??= new HashSet<IOpenApiSchema>();
            if (visited.Contains(schema)) return list;
            visited.Add(schema);

            if (schema.Properties != null && schema.Properties.Any())
            {
                foreach (var prop in schema.Properties)
                {
                    var propName = string.IsNullOrEmpty(prefix) ? prop.Key : $"{prefix}.{prop.Key}";
                    var isRequired = schema.Required != null && schema.Required.Contains(prop.Key);
                    var propType = GetSchemaTypeString(prop.Value);

                    list.Add(new FlatProperty
                    {
                        Name = propName,
                        Type = propType,
                        Required = isRequired,
                        Description = prop.Value.Description ?? ""
                    });

                    if (prop.Value.Type == JsonSchemaType.Object)
                    {
                        list.AddRange(FlattenProperties(prop.Value, propName, depth + 1, visited));
                    }
                    else if (prop.Value.Type == JsonSchemaType.Array && prop.Value.Items?.Type == JsonSchemaType.Object)
                    {
                        list.AddRange(FlattenProperties(prop.Value.Items, $"{propName}[]", depth + 1, visited));
                    }
                }
            }

            visited.Remove(schema);
            return list;
        }
    }
}
