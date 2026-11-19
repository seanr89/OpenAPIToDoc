using System;
using System.IO;

namespace OpenAPIToDocConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run(args, Console.In, Console.Out);
        }

        internal static void Run(string[] args, TextReader input, TextWriter output)
        {
            output.WriteLine("==============================================");
            output.WriteLine("        OpenAPI Specification to Word Doc      ");
            output.WriteLine("==============================================");

            string? inputPath = null;
            string? outputPath = null;

            if (args.Length > 0)
            {
                inputPath = args[0];
                if (args.Length > 1)
                {
                    outputPath = args[1];
                }
            }
            else
            {
                // Interactive Mode
                output.Write("Enter the path to the OpenAPI JSON file: ");
                inputPath = input.ReadLine()?.Trim(' ', '"'); // Trim spaces and quotes
            }

            if (string.IsNullOrEmpty(inputPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                output.WriteLine("Error: Input path cannot be empty.");
                Console.ResetColor();
                return;
            }

            if (!File.Exists(inputPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                output.WriteLine($"Error: File not found at '{inputPath}'.");
                Console.ResetColor();
                return;
            }

            try
            {
                string actualOutputPath = OpenApiToDocGenerator.GenerateWordDoc(inputPath, outputPath);
                
                Console.ForegroundColor = ConsoleColor.Green;
                output.WriteLine("\nSuccess!");
                output.WriteLine($"Word document created at: {Path.GetFullPath(actualOutputPath)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                output.WriteLine($"\nAn error occurred during generation: {ex.Message}");
                output.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
    }
}
