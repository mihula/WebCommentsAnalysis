using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebCommentsAnalysis
{
    class Program
    {
        /// <summary>
        /// Main entry point of the program.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\KEC\WfStepList.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\KEC\CustomWFTaskDetail.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\GenericModules.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\GUI\CreateGenericFrameWizard.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\ARC\AudPredValuePivotList.cs";

            // Set the default path to the modules directory
            var path = args.Length > 0 ? args[0] : @"C:\Provys\pvysdev\_net\src\Provys\Modules";

            // Test Set the module name to "KEC" if provided, otherwise use an empty string
            //var moduleOnly = args.Length > 1 ? args[1] : "KEC";
            // Set the module name to if provided, otherwise use an empty string
            var moduleOnly = args.Length > 1 ? args[1] : "";

            // If a module name is provided, combine it with the path
            if (!string.IsNullOrEmpty(moduleOnly))
                path = Path.Combine(path, moduleOnly);

            // Get all the .cs files in the specified path and its subdirectories
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            // Process each file
            foreach (string file in files)
            {
                // Get the full path of the file
                string filePath = Path.GetFullPath(file);

                // Read the content of the file
                string fileContent = File.ReadAllText(file);

                // Process the file and get the result
                var fi = ProcessFile(filePath);

                // Print the result
                Console.WriteLine(fi);
            }
        }

        /// <summary>
        /// Processes a C# file and extracts information about its classes and methods.
        /// </summary>
        /// <param name="filename">The path to the C# file to process.</param>
        /// <returns>A FileInfo object containing information about the file's classes and methods.</returns>
        static FileInfo ProcessFile(string filename)
        {
            // Read the contents of the file
            string code = File.ReadAllText(filename);
            // Parse the file into a syntax tree
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            // Create a new FileInfo object to store the extracted information
            var fileInfo = new FileInfo
            {
                FullPath = filename,
                ModuleName = Path.GetDirectoryName(filename).Split(Path.DirectorySeparatorChar).Last(),
                FileName = Path.GetFileName(filename),
                Classes = new List<ClassInfo>()
            };
            // Iterate over all class declarations in the file
            foreach (var classNode in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                // Create a new ClassInfo object to store information about the class
                var classInfo = new ClassInfo
                {
                    ClassName = $"class {classNode.Identifier.ValueText}",
                    Methods = new List<MethodInfo>()
                };
                // Extract the Configuration attribute from the class
                var attributes = classNode.AttributeLists.SelectMany(al => al.Attributes);
                var formClassName = attributes.FirstOrDefault(a => a.Name.ToString() == "Configuration")?
                                    .ArgumentList.Arguments.First().Expression.ToString().Trim('"');
                classInfo.FormClassName = formClassName;
                // Extract the WEB comment from the class
                var classStatusComment = classNode.GetLeadingTrivia()
                                            .Select(t => t.ToString().Trim())
                                            .FirstOrDefault(t => t.StartsWith("//WEB:"));

                if (classStatusComment != null)
                {
                    // Parse the WEB comment into status and sub-status
                    var (status, subStatus) = ExtractStatusAndSubStatus(classStatusComment);
                    classInfo.ClassStatus = status;
                    classInfo.ClassSubStatus = subStatus;
                }
                // Iterate over all method declarations in the class
                foreach (var method in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    // Create a new MethodInfo object to store information about the method
                    var methodInfo = new MethodInfo
                    {
                        MethodName = $"{method.ReturnType} {method.Identifier.ValueText}({string.Join(", ", method.ParameterList.Parameters)})",
                        MethodLineCount = CalculateMethodLineCount(method),
                    };
                    // Extract the WEB comment from the method
                    var methodStatusComment = method.GetLeadingTrivia()
                                                .Select(t => t.ToString().Trim())
                                                .FirstOrDefault(t => t.StartsWith("//WEB:"));
                    if (methodStatusComment != null)
                    {
                        // Parse the WEB comment into status and sub-status
                        var (status, subStatus) = ExtractStatusAndSubStatus(methodStatusComment);
                        methodInfo.MethodStatus = status;
                        methodInfo.MethodSubStatus = subStatus;
                    }
                    // Add the method to the class's method list
                    classInfo.Methods.Add(methodInfo);
                }
                // Add the class to the file's class list
                fileInfo.Classes.Add(classInfo);
            }
            return fileInfo;
        }

        /// <summary>
        /// Extracts status and sub-status from a WEB comment.
        /// </summary>
        /// <param name="comment">The WEB comment to parse.</param>
        /// <returns>An anonymous object containing the status and sub-status.</returns>
        static (string Status, string SubStatus) ExtractStatusAndSubStatus(string comment)
        {
            var parts = comment.Substring(6).Split(':');
            return (parts[0], parts.Length > 1 ? string.Join(":", parts.Skip(1)) : null);
        }

        /// <summary>
        /// Calculates the number of lines in a method, excluding empty lines and lines starting with '//'.
        /// </summary>
        /// <param name="method">The method declaration syntax to calculate the line count for.</param>
        /// <returns>The number of lines in the method.</returns>
        static int CalculateMethodLineCount(MethodDeclarationSyntax method)
        {
            // Get the method body as an array of lines, removing empty lines
            var lines = method.Body?.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // If the method body is null or empty, assume it has 1 line
            if (lines == null) return 1;

            // Count the number of lines that are not empty and do not start with '//'
            return lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"));
        }
    }

    class FileInfo
    {
        public string FullPath { get; set; }
        public string ModuleName { get; set; }
        public string FileName { get; set; }
        public List<ClassInfo> Classes { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();
            var fileString = $"{ModuleName}\t{FileName}";
            foreach (var c in Classes)
            {
                foreach (var m in c.Methods)
                {
                    result.AppendLine($"{fileString}\t{c.ToString()}\t{m.ToString()}");
                }
            }
            return result.ToString();
        }
    }

    class ClassInfo
    {
        public string ClassName { get; set; }
        public string FormClassName { get; set; }
        public string ClassStatus { get; set; }
        public string ClassSubStatus { get; set; }
        public List<MethodInfo> Methods { get; set; }

        public override string ToString()
        {
            return $"{ClassName}\t{FormClassName}\t{ClassStatus}\t{ClassSubStatus}";
        }
    }

    class MethodInfo
    {
        public string MethodName { get; set; }
        public string MethodStatus { get; set; }
        public string MethodSubStatus { get; set; }
        public int MethodLineCount { get; set; }

        public override string ToString()
        {
            return $"{MethodName}\t{MethodStatus}\t{MethodSubStatus}\t{MethodLineCount}";
        }
    }
}