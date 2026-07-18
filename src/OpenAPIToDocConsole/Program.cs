using System;
using System.IO;

namespace OpenAPIToDocConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("        OpenAPI Specification to Word Doc      ");
            Console.WriteLine("==============================================");

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
                Console.Write("Enter the path to the OpenAPI JSON file: ");
                inputPath = Console.ReadLine()?.Trim(' ', '"'); // Trim spaces and quotes
            }

            if (string.IsNullOrEmpty(inputPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Input path cannot be empty.");
                Console.ResetColor();
                return;
            }

            if (!File.Exists(inputPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found at '{inputPath}'.");
                Console.ResetColor();
                return;
            }

            try
            {
                string actualOutputPath = OpenApiToDocGenerator.GenerateWordDoc(inputPath, outputPath);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nSuccess!");
                Console.WriteLine($"Word document created at: {Path.GetFullPath(actualOutputPath)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nAn error occurred during generation: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
    }
}
