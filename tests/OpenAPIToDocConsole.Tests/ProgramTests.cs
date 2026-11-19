using System;
using System.IO;

namespace OpenAPIToDocConsole.Tests
{
    public class ProgramTests : IDisposable
    {
        private readonly string _tempDir;

        public ProgramTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"OpenApiToDocProgramTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private string WriteMinimalSpec(string fileName = "spec.json")
        {
            string path = Path.Combine(_tempDir, fileName);
            File.WriteAllText(path, """
                {
                  "openapi": "3.0.1",
                  "info": {
                    "title": "CLI Test API",
                    "version": "1.0.0"
                  },
                  "paths": {}
                }
                """);
            return path;
        }

        [Fact]
        public void Run_EmptyInput_WritesEmptyPathError()
        {
            var input = new StringReader(string.Empty);
            var output = new StringWriter();

            Program.Run(Array.Empty<string>(), input, output);

            string text = output.ToString();
            Assert.Contains("Enter the path to the OpenAPI JSON file:", text);
            Assert.Contains("Error: Input path cannot be empty.", text);
        }

        [Fact]
        public void Run_NonExistentFile_WritesFileNotFoundError()
        {
            string missingPath = Path.Combine(_tempDir, "missing.json");
            var output = new StringWriter();

            Program.Run(new[] { missingPath }, new StringReader(string.Empty), output);

            Assert.Contains($"Error: File not found at '{missingPath}'.", output.ToString());
        }

        [Fact]
        public void Run_ValidArgs_CreatesDocAndWritesSuccess()
        {
            string specPath = WriteMinimalSpec();
            string outPath = Path.Combine(_tempDir, "cli-out.docx");
            var output = new StringWriter();

            Program.Run(new[] { specPath, outPath }, new StringReader(string.Empty), output);

            string text = output.ToString();
            Assert.True(File.Exists(outPath));
            Assert.Contains("Success!", text);
            Assert.Contains($"Word document created at: {Path.GetFullPath(outPath)}", text);
        }

        [Fact]
        public void Run_InteractiveModeWithQuotedPath_TrimsQuotesAndCreatesDoc()
        {
            string specPath = WriteMinimalSpec();
            string expectedOutPath = Path.Combine(_tempDir, "CLI Test API_1.0.0.docx");
            var input = new StringReader($"  \"{specPath}\"  ");
            var output = new StringWriter();

            Program.Run(Array.Empty<string>(), input, output);

            string text = output.ToString();
            Assert.True(File.Exists(expectedOutPath));
            Assert.Contains("Success!", text);
            Assert.Contains($"Word document created at: {Path.GetFullPath(expectedOutPath)}", text);
        }

        [Fact]
        public void Run_InvalidOpenApiFile_WritesGenerationError()
        {
            string invalidPath = Path.Combine(_tempDir, "invalid.json");
            File.WriteAllText(invalidPath, "{}");
            var output = new StringWriter();

            Program.Run(new[] { invalidPath }, new StringReader(string.Empty), output);

            Assert.Contains("An error occurred during generation:", output.ToString());
        }
    }
}
