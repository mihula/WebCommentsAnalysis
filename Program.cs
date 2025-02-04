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
        static void Main(string[] args)
        {
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\KEC\WfStepList.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\KEC\CustomWFTaskDetail.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\GenericModules.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\GUI\CreateGenericFrameWizard.cs";
            //string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\ARC\AudPredValuePivotList.cs";

            var path = args.Length > 0 ? args[0] : @"C:\Provys\pvysdev\_net\src\Provys\Modules";
            var moduleOnly = args.Length > 1 ? args[1] : "KEC";
            //var moduleOnly = args.Length > 1 ? args[1] : string.Empty;

            if (!string.IsNullOrEmpty(moduleOnly))
                path = Path.Combine(path, moduleOnly);

            // nacti soubory *.cs z path, pokud je vyplnen moduleOnly tak jen z toho zadaneho adresare
            // pak alayzuj jeden soubor, vysledek analyzy spoj do vystupu analyzy s jeho cestou jako klicem

            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string filePath = Path.GetFullPath(file);
                string fileContent = File.ReadAllText(file);

                var fi = ProcessFile(filePath);
                Console.WriteLine(fi);
            }
        }

        static FileInfo ProcessFile(string filename)
        {
            string code = File.ReadAllText(filename);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var fileInfo = new FileInfo
            {
                FullPath = filename,
                ModuleName = Path.GetDirectoryName(filename).Split(Path.DirectorySeparatorChar).Last(),
                FileName = Path.GetFileName(filename),
                Classes = new List<ClassInfo>()
            };

            foreach (var classNode in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classInfo = new ClassInfo
                {
                    ClassName = $"class {classNode.Identifier.ValueText}",
                    Methods = new List<MethodInfo>()
                };

                var attributes = classNode.AttributeLists.SelectMany(al => al.Attributes);
                var formClassName = attributes.FirstOrDefault(a => a.Name.ToString() == "Configuration")?
                                    .ArgumentList.Arguments.First().Expression.ToString().Trim('"');

                classInfo.FormClassName = formClassName;

                var classStatusComment = classNode.GetLeadingTrivia()
                                            .Select(t => t.ToString().Trim())
                                            .FirstOrDefault(t => t.StartsWith("//WEB:"));

                if (classStatusComment != null)
                {
                    var parts = classStatusComment.Substring(6).Split(':');
                    classInfo.ClassStatus = parts[0];
                    if (parts.Length > 1)
                    {
                        classInfo.ClassSubStatus = string.Join(":", parts.Skip(1));
                    }
                }

                foreach (var method in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodInfo = new MethodInfo
                    {
                        MethodName = $"{method.ReturnType} {method.Identifier.ValueText}({string.Join(", ", method.ParameterList.Parameters)})",
                        MethodLineCount = CalculateMethodLineCount(method),
                    };

                    var methodStatusComment = method.GetLeadingTrivia()
                                                .Select(t => t.ToString().Trim())
                                                .FirstOrDefault(t => t.StartsWith("//WEB:"));

                    if (methodStatusComment != null)
                    {
                        var parts = methodStatusComment.Substring(6).Split(':');
                        methodInfo.MethodStatus = parts[0];
                        if (parts.Length > 1)
                        {
                            methodInfo.MethodSubStatus = string.Join(":", parts.Skip(1));
                        }
                    }

                    classInfo.Methods.Add(methodInfo);
                }

                fileInfo.Classes.Add(classInfo);
            }

            return fileInfo;
        }

        static int CalculateMethodLineCount(MethodDeclarationSyntax method)
        {
            var lines = method.Body?.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines == null) return 1;

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