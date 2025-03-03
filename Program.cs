using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            var moduleOnly = args.Length > 1 ? args[1] : "";
            // Set the module name to if provided, otherwise use an empty string
            //var moduleOnly = args.Length > 1 ? args[1] : "";

            // If a module name is provided, combine it with the path
            if (!string.IsNullOrEmpty(moduleOnly))
                path = Path.Combine(path, moduleOnly);

            // Get all the .cs files in the specified path and its subdirectories
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            // result
            var fileInfos = new List<AnalysisFileInfo>();

            // Process each file
            foreach (string file in files)
            {
                // Get the full path of the file
                string filePath = Path.GetFullPath(file);

                // Read the content of the file
                string fileContent = File.ReadAllText(file);

                // Process the file and get the result
                var fi = ProcessFile(filePath);

                fileInfos.Add(fi);

                //Console.WriteLine($"{filePath}: {fi.Classes.Count()}");

                //// Print the result
                //var fiAsString = fi.ToString();
                //if (!string.IsNullOrEmpty(fiAsString))
                //    Console.WriteLine(fi);
            }

            // necham si jen ty co nemaji entity ted
            var classesWithEntity = fileInfos.SelectMany(fi => fi.Classes).Where(c => !string.IsNullOrEmpty(c.EntityName)).ToList();
            var classesWithoutEntity = fileInfos.SelectMany(fi => fi.Classes).Where(c => string.IsNullOrEmpty(c.EntityName)).ToList();

            // dohledat entity
            var classesToProcess = new Queue<AnalysisClassInfo>(classesWithoutEntity);
            var classesUnprocessed = new Queue<AnalysisClassInfo>();
            var lastSizeOfUnprocessed = classesToProcess.Count();

            while (true)
            {
                //Console.WriteLine($"Unprocessed classes: {lastSizeOfUnprocessed}");
                while (classesToProcess.Count() > 0)
                {
                    var c = classesToProcess.Dequeue();

                    // zkus najit podobnou classu
                    var ci = FindSimilarClass(c.ClassName, classesWithEntity);
                    if (ci != null)
                    {
                        c.EntityName = ci.EntityName;
                        classesWithEntity.Add(c);
                        continue;
                    }

                    // zkus tipnout entitu podle classname
                    var entityName = GuessEntityByClassName(c.ClassName);
                    if (!string.IsNullOrEmpty(entityName))
                    {
                        c.EntityName = entityName;
                        classesWithEntity.Add(c);
                        continue;
                    }

                    // nenasel jsem
                    classesUnprocessed.Enqueue(c);
                }

                if (lastSizeOfUnprocessed == classesUnprocessed.Count())
                {
                    break; // neni uz co resit
                }

                // prehazu a jedu znova
                lastSizeOfUnprocessed = classesUnprocessed.Count();
                classesToProcess = classesUnprocessed;
                classesUnprocessed = new Queue<AnalysisClassInfo>();
            }

            //Console.WriteLine($"WITH ENTITY: {classesWithEntity.Count()}");
            //foreach (var c in classesWithEntity)
            //{
            //    var ciAsString = c.ToString();
            //    if (!string.IsNullOrEmpty(ciAsString))
            //        Console.WriteLine(ciAsString);
            //}

            //for (var i = 0; i < 10; i++) Console.WriteLine();

            //Console.WriteLine($"WITHOUT ENTITY: {classesUnprocessed.Count()}");
            //foreach (var c in classesUnprocessed)
            //{
            //    var ciAsString = c.ToString();
            //    if (!string.IsNullOrEmpty(ciAsString))
            //        Console.Write(ciAsString);
            //}

            // ALL
            Console.WriteLine("MODULE\tFILENAME\tENTITY\tCLASSNAME\tFORMCLASSNAME\tCLASSSTATUS\tCLASSSUBSTATUS\tMETHODNAME\tSTATUS\tSUBSTATUS\tSIZE");
            foreach (var fi in fileInfos)
            {
                var fiAsString = PrintInfo.Print(fi);
                if (!string.IsNullOrEmpty(fiAsString))
                    Console.Write(fi);
            }
        }

        private static AnalysisClassInfo FindSimilarClass(string className, List<AnalysisClassInfo> classesWithEntity)
        {
            // pokud je to partial class tak nemusi mit u sebe formclassname a pritom uz ho znam i entitu
            var partialClass = classesWithEntity.FirstOrDefault(x => x.ClassName == className);
            if (partialClass != null) return partialClass;

            if (className.EndsWith("EditList"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("EditList", "List"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("Module"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("Module", "List"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("Module"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("Module", "Detail"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("SwitchModule"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("SwitchModule", "List"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("CommonModule"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("CommonModule", "Module"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("BaseModule"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("BaseModule", "Module"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("CrossModule"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("CrossModule", "Module"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("CrossModule"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("CrossModule", "List"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("SwitchModule"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("SwitchModule", "Detail"));
                if (ci != null) return ci;
            }
            if (className.EndsWith("DetailSwitch"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("DetailSwitch", "Detail"));
                if (ci != null) return ci;
            }
            if (className.StartsWith("Common"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("Common", ""));
                if (ci != null) return ci;
            }
            if (className.StartsWith("Custom"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("Custom", ""));
                if (ci != null) return ci;
            }
            if (className.Contains("Base"))
            {
                var ci = classesWithEntity.FirstOrDefault(x => x.ClassName == className.Replace("Base", ""));
                if (ci != null) return ci;
            }

            return null;
        }

        private static string GuessEntityByClassName(string className)
        {
            string[] entityNamesSubstrings = {
                "TECHINFO", "RESALLOC", "SUBJSUBTYPE", "REFERENCE", "REPORT", "COMPPFL", "COMPCFG", "XCOMP", "CONFOBJ",
                "COMMORDER", "COMMBLOCK", "TXEVENT", "TXSLOT", "PRODUSAGE", "VODREQ"
            };
            var upClassName = className.ToUpperInvariant();
            foreach (var entityName in entityNamesSubstrings)
            {
                if (upClassName.Contains(entityName))
                    return entityName;
            }
            return null;
        }

        /// <summary>
        /// Processes a C# file and extracts information about its classes and methods.
        /// </summary>
        /// <param name="filename">The path to the C# file to process.</param>
        /// <returns>A AnalysisFileInfo object containing information about the file's classes and methods.</returns>
        static AnalysisFileInfo ProcessFile(string filename)
        {
            // Read the contents of the file
            string code = File.ReadAllText(filename);
            // Parse the file into a syntax tree
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            // Create a new AnalysisFileInfo object to store the extracted information
            var fileInfo = new AnalysisFileInfo
            {
                FullPath = filename,
                ModuleName = Path.GetDirectoryName(filename).Split(Path.DirectorySeparatorChar).Last(),
                FileName = Path.GetFileName(filename),
                Classes = new List<AnalysisClassInfo>()
            };
            // Iterate over all class declarations in the file
            foreach (var classNode in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                // Create a new AnalysisClassInfo object to store information about the class
                var classInfo = new AnalysisClassInfo
                {
                    FileInfo = fileInfo,
                    ClassName = classNode.Identifier.ValueText, // $"class {classNode.Identifier.ValueText}",
                    Methods = new List<AnalysisMethodInfo>()
                };

                // Extract the Configuration attribute from the class
                var attributes = classNode.AttributeLists.SelectMany(al => al.Attributes);
                var formClassName = attributes.FirstOrDefault(a => a.Name.ToString() == "Configuration")?
                                    .ArgumentList.Arguments.First().Expression.ToString().Trim('"');
                classInfo.FormClassName = formClassName;
                classInfo.EntityName = EntityHelper.FormClassToEntity(formClassName);

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
                    // Create a new AnalysisMethodInfo object to store information about the method
                    var methodInfo = new AnalysisMethodInfo
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
}