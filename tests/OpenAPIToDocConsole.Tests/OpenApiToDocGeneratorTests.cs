using System;
using System.IO;
using System.Linq;
using OfficeIMO.Drawing;
using OfficeIMO.Word;

namespace OpenAPIToDocConsole.Tests
{
    public class OpenApiToDocGeneratorTests : IDisposable
    {
        private readonly string _tempDir;

        public OpenApiToDocGeneratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"OpenApiToDocTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private string WriteTempSpec(string fileName, string json)
        {
            string path = Path.Combine(_tempDir, fileName);
            File.WriteAllText(path, json);
            return path;
        }

        [Fact]
        public void GenerateWordDoc_MissingInputFile_ThrowsFileNotFoundException()
        {
            string missingPath = Path.Combine(_tempDir, "does-not-exist.json");

            Assert.Throws<FileNotFoundException>(() => OpenApiToDocGenerator.GenerateWordDoc(missingPath));
        }

        [Fact]
        public void GenerateWordDoc_InvalidOpenApiContent_ThrowsInvalidDataException()
        {
            string invalidPath = WriteTempSpec("invalid.json", "{}");

            Assert.Throws<InvalidDataException>(() => OpenApiToDocGenerator.GenerateWordDoc(invalidPath));
        }

        [Fact]
        public void GenerateWordDoc_DefaultOutputPath_SavesSanitizedFilenameNextToInput()
        {
            string specPath = WriteTempSpec("spec.json", """
                {
                  "openapi": "3.0.1",
                  "info": {
                    "title": "Orders/API",
                    "version": "v2.0"
                  },
                  "paths": {}
                }
                """);

            string actualPath = OpenApiToDocGenerator.GenerateWordDoc(specPath);

            string expectedPath = Path.Combine(_tempDir, "Orders_API_v2.0.docx");
            Assert.Equal(expectedPath, actualPath);
            Assert.True(File.Exists(actualPath));
        }

        [Fact]
        public void GenerateWordDoc_DirectoryOutputPath_SavesSanitizedFilenameInsideDirectory()
        {
            string specPath = WriteTempSpec("spec.json", """
                {
                  "openapi": "3.0.1",
                  "info": {
                    "title": "Billing/API",
                    "version": "1.0.0"
                  },
                  "paths": {}
                }
                """);
            string outDir = Path.Combine(_tempDir, "output");
            Directory.CreateDirectory(outDir);

            string actualPath = OpenApiToDocGenerator.GenerateWordDoc(specPath, outDir);

            string expectedPath = Path.Combine(outDir, "Billing_API_1.0.0.docx");
            Assert.Equal(expectedPath, actualPath);
            Assert.True(File.Exists(actualPath));
        }

        [Fact]
        public void GenerateWordDoc_ExplicitFileOutputPath_SavesToExactPath()
        {
            string specPath = WriteTempSpec("spec.json", """
                {
                  "openapi": "3.0.1",
                  "info": {
                    "title": "Custom API",
                    "version": "1.0.0"
                  },
                  "paths": {}
                }
                """);
            string explicitPath = Path.Combine(_tempDir, "custom-name.docx");

            string actualPath = OpenApiToDocGenerator.GenerateWordDoc(specPath, explicitPath);

            Assert.Equal(explicitPath, actualPath);
            Assert.True(File.Exists(explicitPath));
        }

        [Fact]
        public void GenerateWordDoc_MinimalSpecWithNoPaths_RendersDefaultsAndEmptyMessage()
        {
            string specPath = WriteTempSpec("minimal.json", """
                {
                  "openapi": "3.0.1",
                  "info": {},
                  "paths": {}
                }
                """);
            string outputPath = Path.Combine(_tempDir, "minimal.docx");

            OpenApiToDocGenerator.GenerateWordDoc(specPath, outputPath);

            using var doc = WordDocument.Load(outputPath, readOnly: true);
            var texts = doc.Paragraphs.Select(p => p.Text).ToList();

            Assert.Contains("API Specification", texts);
            Assert.Contains("Version 1.0.0", texts);
            Assert.Contains("No API endpoints or operations found in the specification.", texts);
            Assert.DoesNotContain("Overview", texts);
            Assert.DoesNotContain("Contact Information", texts);
            Assert.DoesNotContain("Base Server URLs", texts);
            Assert.DoesNotContain("Detailed Functional Endpoints", texts);
        }

        [Fact]
        public void GenerateWordDoc_RichSpec_RendersMetadataTagsEndpointsAndTables()
        {
            string specPath = WriteTempSpec("rich.json", """
                {
                  "openapi": "3.0.1",
                  "info": {
                    "title": "Order API",
                    "version": "1.2.0",
                    "description": "Manages customer orders.",
                    "contact": {
                      "name": "API Team",
                      "email": "api@example.com"
                    }
                  },
                  "servers": [
                    {
                      "url": "https://api.example.com",
                      "description": "Production"
                    }
                  ],
                  "tags": [
                    {
                      "name": "Users",
                      "description": "User operations"
                    }
                  ],
                  "paths": {
                    "/users/{id}": {
                      "get": {
                        "tags": [ "Users" ],
                        "summary": "Get user",
                        "description": "Fetch a user by ID.",
                        "parameters": [
                          {
                            "name": "id",
                            "in": "path",
                            "required": true,
                            "description": "User ID",
                            "schema": { "type": "string", "format": "uuid" }
                          },
                          {
                            "name": "verbose",
                            "in": "query",
                            "required": false,
                            "description": "Include details",
                            "schema": { "type": "boolean" }
                          }
                        ],
                        "responses": {
                          "200": {
                            "description": "Success",
                            "content": {
                              "application/json": {
                                "schema": { "$ref": "#/components/schemas/User" }
                              }
                            }
                          },
                          "404": {
                            "description": "Not found"
                          }
                        }
                      }
                    },
                    "/users": {
                      "post": {
                        "tags": [ "Users" ],
                        "summary": "Create user",
                        "requestBody": {
                          "description": "User payload",
                          "content": {
                            "application/json": {
                              "schema": { "$ref": "#/components/schemas/CreateUserRequest" }
                            }
                          }
                        },
                        "responses": {
                          "201": {
                            "description": "Created",
                            "content": {
                              "application/json": {
                                "schema": { "$ref": "#/components/schemas/User" }
                              }
                            }
                          }
                        }
                      }
                    },
                    "/notes": {
                      "put": {
                        "tags": [ "Users" ],
                        "summary": "Update note",
                        "requestBody": {
                          "content": {
                            "text/plain": {
                              "schema": { "type": "string" }
                            }
                          }
                        },
                        "responses": {
                          "204": {
                            "description": "No Content"
                          }
                        }
                      }
                    },
                    "/health": {
                      "delete": {
                        "responses": {
                          "200": {
                            "description": "Healthy"
                          }
                        }
                      }
                    }
                  },
                  "components": {
                    "schemas": {
                      "CreateUserRequest": {
                        "type": "object",
                        "required": [ "name" ],
                        "properties": {
                          "name": {
                            "type": "string",
                            "description": "Full name"
                          },
                          "address": {
                            "type": "object",
                            "required": [ "street" ],
                            "properties": {
                              "street": { "type": "string" }
                            }
                          },
                          "items": {
                            "type": "array",
                            "items": {
                              "type": "object",
                              "properties": {
                                "id": { "type": "integer", "format": "int32" }
                              }
                            }
                          }
                        }
                      },
                      "User": {
                        "type": "object",
                        "properties": {
                          "id": { "type": "string", "format": "uuid" }
                        }
                      }
                    }
                  }
                }
                """);
            string outputPath = Path.Combine(_tempDir, "rich.docx");

            OpenApiToDocGenerator.GenerateWordDoc(specPath, outputPath);

            using var doc = WordDocument.Load(outputPath, readOnly: true);
            var paragraphs = doc.Paragraphs.ToList();
            var texts = paragraphs.Select(p => p.Text).ToList();

            // Metadata and server details
            Assert.Contains("Order API", texts);
            Assert.Contains("Version 1.2.0", texts);
            Assert.Contains("Overview", texts);
            Assert.Contains("Manages customer orders.", texts);
            Assert.Contains("Contact Information", texts);
            Assert.Contains("Name: API Team", texts);
            Assert.Contains("Email: api@example.com", texts);
            Assert.Contains("Base Server URLs", texts);
            Assert.Contains("- https://api.example.com", texts);
            Assert.Contains(" (Production)", texts);

            // Tag sections and alphabetical ordering ("General Services" before "Users")
            int generalIdx = texts.IndexOf("General Services");
            int usersIdx = texts.IndexOf("Users");
            Assert.True(generalIdx >= 0 && usersIdx > generalIdx);
            Assert.Contains("User operations", texts);

            // Fallback heading/description for untagged endpoint without summary/description
            Assert.Contains("DELETE /health", texts);
            Assert.Contains("No description provided for this endpoint.", texts);

            // Method colors on individual runs
            Assert.Contains(paragraphs, p => p.Text == "[DELETE] " && p.Color == OfficeColor.Red);
            Assert.Contains(paragraphs, p => p.Text == "[PUT] " && p.Color == OfficeColor.Orange);
            Assert.Contains(paragraphs, p => p.Text == "[POST] " && p.Color == OfficeColor.Green);
            Assert.Contains(paragraphs, p => p.Text == "[GET] " && p.Color == OfficeColor.Blue);

            // Primitive request body fallback
            Assert.Contains("Format: text/plain", texts);
            Assert.Contains("Expected Data Type: string", texts);

            // Input Parameters table
            WordTable paramTable = Assert.Single(doc.Tables, t => t.Rows[0].Cells[0].Paragraphs[0].Text == "Parameter");
            Assert.Equal("Location", paramTable.Rows[0].Cells[1].Paragraphs[0].Text);
            Assert.Equal("Type", paramTable.Rows[0].Cells[2].Paragraphs[0].Text);
            Assert.Equal("Required", paramTable.Rows[0].Cells[3].Paragraphs[0].Text);
            Assert.Equal("Description", paramTable.Rows[0].Cells[4].Paragraphs[0].Text);

            Assert.Equal("id", paramTable.Rows[1].Cells[0].Paragraphs[0].Text);
            Assert.Equal("string (uuid)", paramTable.Rows[1].Cells[2].Paragraphs[0].Text);
            Assert.Equal("Yes", paramTable.Rows[1].Cells[3].Paragraphs[0].Text);
            Assert.Equal(OfficeColor.Red, paramTable.Rows[1].Cells[3].Paragraphs[0].Color);
            Assert.True(paramTable.Rows[1].Cells[3].Paragraphs[0].Bold);
            Assert.Equal("verbose", paramTable.Rows[2].Cells[0].Paragraphs[0].Text);
            Assert.Equal("No", paramTable.Rows[2].Cells[3].Paragraphs[0].Text);

            // Request Body flattened table
            WordTable bodyTable = Assert.Single(doc.Tables, t => t.Rows[0].Cells[0].Paragraphs[0].Text == "Property");
            var bodyRows = bodyTable.Rows.Skip(1).ToList();
            Assert.Contains(bodyRows, r => r.Cells[0].Paragraphs[0].Text == "name"
                && r.Cells[2].Paragraphs[0].Text == "Yes"
                && r.Cells[2].Paragraphs[0].Color == OfficeColor.Red);
            Assert.Contains(bodyRows, r => r.Cells[0].Paragraphs[0].Text == "address.street"
                && r.Cells[2].Paragraphs[0].Text == "Yes"
                && r.Cells[2].Paragraphs[0].Color == OfficeColor.Red);
            Assert.Contains(bodyRows, r => r.Cells[0].Paragraphs[0].Text == "items[].id"
                && r.Cells[1].Paragraphs[0].Text == "integer (int32)"
                && r.Cells[2].Paragraphs[0].Text == "No");

            // Responses tables (status colors, schema ref, and "None")
            WordTable getUsersResponses = doc.Tables.Last();
            Assert.Equal("Status Code", getUsersResponses.Rows[0].Cells[0].Paragraphs[0].Text);
            Assert.Equal("200", getUsersResponses.Rows[1].Cells[0].Paragraphs[0].Text);
            Assert.Equal(OfficeColor.Green, getUsersResponses.Rows[1].Cells[0].Paragraphs[0].Color);
            Assert.Equal("User", getUsersResponses.Rows[1].Cells[2].Paragraphs[0].Text);

            Assert.Equal("404", getUsersResponses.Rows[2].Cells[0].Paragraphs[0].Text);
            Assert.Equal(OfficeColor.Red, getUsersResponses.Rows[2].Cells[0].Paragraphs[0].Color);
            Assert.Equal("None", getUsersResponses.Rows[2].Cells[2].Paragraphs[0].Text);
        }
    }
}
