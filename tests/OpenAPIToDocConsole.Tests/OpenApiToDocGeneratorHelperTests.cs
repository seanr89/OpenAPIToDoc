using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using OfficeIMO.Drawing;

namespace OpenAPIToDocConsole.Tests
{
    public class OpenApiToDocGeneratorHelperTests
    {
        [Fact]
        public void SanitizeFileName_ValidName_ReturnsUnchanged()
        {
            const string validName = "Spec API_v1.0.0.docx";

            string result = OpenApiToDocGenerator.SanitizeFileName(validName);

            Assert.Equal(validName, result);
        }

        [Fact]
        public void SanitizeFileName_InvalidCharacters_ReplacedWithUnderscore()
        {
            string withSlash = OpenApiToDocGenerator.SanitizeFileName("Orders/API_v1.0.docx");
            Assert.Equal("Orders_API_v1.0.docx", withSlash);

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string raw = "prefix" + new string(invalidChars) + "suffix";
            string expected = "prefix" + new string('_', invalidChars.Length) + "suffix";

            string sanitized = OpenApiToDocGenerator.SanitizeFileName(raw);

            Assert.Equal(expected, sanitized);
        }

        public static IEnumerable<object[]> MethodColorCases()
        {
            yield return new object[] { "GET", OfficeColor.Blue };
            yield return new object[] { "get", OfficeColor.Blue };
            yield return new object[] { "POST", OfficeColor.Green };
            yield return new object[] { "PUT", OfficeColor.Orange };
            yield return new object[] { "DELETE", OfficeColor.Red };
            yield return new object[] { "PATCH", OfficeColor.Black };
        }

        [Theory]
        [MemberData(nameof(MethodColorCases))]
        public void GetMethodColor_MapsSupportedAndDefaultMethods(string methodName, OfficeColor expectedColor)
        {
            var method = new HttpMethod(methodName);

            OfficeColor actual = OpenApiToDocGenerator.GetMethodColor(method);

            Assert.Equal(expectedColor, actual);
        }

        [Fact]
        public void GetSchemaTypeString_NullSchema_ReturnsAny()
        {
            string result = OpenApiToDocGenerator.GetSchemaTypeString(null);

            Assert.Equal("any", result);
        }

        [Fact]
        public void GetSchemaTypeString_SchemaReference_ReturnsReferenceId()
        {
            var schemaRef = new OpenApiSchemaReference("Office", null);

            string result = OpenApiToDocGenerator.GetSchemaTypeString(schemaRef);

            Assert.Equal("Office", result);
        }

        [Theory]
        [InlineData(JsonSchemaType.String, null, "string")]
        [InlineData(JsonSchemaType.Integer, null, "integer")]
        [InlineData(JsonSchemaType.Boolean, null, "boolean")]
        [InlineData(null, null, "object")]
        [InlineData(JsonSchemaType.String, "uuid", "string (uuid)")]
        [InlineData(JsonSchemaType.Integer, "int32", "integer (int32)")]
        public void GetSchemaTypeString_PrimitiveAndFormattedTypes_FormatsCorrectly(
            JsonSchemaType? type,
            string? format,
            string expected)
        {
            var schema = new OpenApiSchema
            {
                Type = type,
                Format = format
            };

            string result = OpenApiToDocGenerator.GetSchemaTypeString(schema);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetSchemaTypeString_ArraySchemas_FormatsListPrefix()
        {
            var primitiveArray = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = new OpenApiSchema { Type = JsonSchemaType.String }
            };
            var referenceArray = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = new OpenApiSchemaReference("Office", null)
            };
            var untypedArray = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = null
            };

            Assert.Equal("List of string", OpenApiToDocGenerator.GetSchemaTypeString(primitiveArray));
            Assert.Equal("List of Office", OpenApiToDocGenerator.GetSchemaTypeString(referenceArray));
            Assert.Equal("List of any", OpenApiToDocGenerator.GetSchemaTypeString(untypedArray));
        }

        [Fact]
        public void FlattenProperties_FlatObject_PreservesRequiredFlagsAndDescriptions()
        {
            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "id" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["id"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid",
                        Description = "Unique identifier"
                    },
                    ["notes"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Description = null
                    }
                }
            };

            List<OpenApiToDocGenerator.FlatProperty> result = OpenApiToDocGenerator.FlattenProperties(schema);

            Assert.Equal(2, result.Count);

            Assert.Equal("id", result[0].Name);
            Assert.Equal("string (uuid)", result[0].Type);
            Assert.True(result[0].Required);
            Assert.Equal("Unique identifier", result[0].Description);

            Assert.Equal("notes", result[1].Name);
            Assert.Equal("string", result[1].Type);
            Assert.False(result[1].Required);
            Assert.Equal("", result[1].Description);
        }

        [Fact]
        public void FlattenProperties_NestedObjectAndObjectArray_UsesDottedAndBracketNotation()
        {
            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["address"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "street" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["street"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    },
                    ["items"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                ["id"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                            }
                        }
                    }
                }
            };

            List<OpenApiToDocGenerator.FlatProperty> result = OpenApiToDocGenerator.FlattenProperties(schema);

            Assert.Equal(
                new[] { "address", "address.street", "items", "items[].id" },
                result.ConvertAll(p => p.Name));
            Assert.True(result[1].Required);
            Assert.Equal("integer (int32)", result[3].Type);
        }

        [Fact]
        public void FlattenProperties_ExceedsMaxDepth_StopsAfterDepthThree()
        {
            var level4 = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["l4"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            };
            var level3 = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["l3"] = level4
                }
            };
            var level2 = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["l2"] = level3
                }
            };
            var level1 = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["l1"] = level2
                }
            };
            var root = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["l0"] = level1
                }
            };

            List<OpenApiToDocGenerator.FlatProperty> result = OpenApiToDocGenerator.FlattenProperties(root);
            List<string> names = result.ConvertAll(p => p.Name);

            Assert.Equal(new[] { "l0", "l0.l1", "l0.l1.l2", "l0.l1.l2.l3" }, names);
            Assert.DoesNotContain("l0.l1.l2.l3.l4", names);
        }

        [Fact]
        public void FlattenProperties_CircularReference_TerminatesAndKeepsSiblings()
        {
            var nodeSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>()
            };
            nodeSchema.Properties["self"] = nodeSchema;
            nodeSchema.Properties["label"] = new OpenApiSchema { Type = JsonSchemaType.String };

            List<OpenApiToDocGenerator.FlatProperty> result = OpenApiToDocGenerator.FlattenProperties(nodeSchema);
            List<string> names = result.ConvertAll(p => p.Name);

            Assert.Equal(new[] { "self", "label" }, names);
        }
    }
}
