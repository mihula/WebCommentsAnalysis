using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;

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
            string filename = @"c:\Provys\pvysdev\_net\src\Provys\Modules\ARC\AudPredValuePivotList.cs";

            var fi = ProcessFile(filename);
            Console.WriteLine(fi);
        }

        static FileInfo ProcessFile(string filename)
        {
            string code = File.ReadAllText(filename);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            var fileInfo = new FileInfo
            {
                FileName = filename,
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
            if (lines == null) return 0;

            return lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"));
        }
    }

    class FileInfo
    {
        public string FileName { get; set; }
        public List<ClassInfo> Classes { get; set; }

        public override string ToString()
        {
            return $@"FileInfo: {FileName}
  {string.Join(Environment.NewLine + "  ", Classes)}";
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
            return $@"ClassName: {ClassName}
  FormClassName: {FormClassName}
  ClassStatus: {ClassStatus}
  ClassSubStatus: {ClassSubStatus}
  Methods:
    {string.Join(Environment.NewLine + "    ", Methods)}";
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
            return $@"MethodName: {MethodName}
    MethodStatus: {MethodStatus}
    MethodSubStatus: {MethodSubStatus}
    MethodLineCount: {MethodLineCount}";
        }
    }
}