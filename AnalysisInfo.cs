using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebCommentsAnalysis
{
    internal static class PrintInfo
    {
        public static string Print(AnalysisFileInfo analysisFileInfo)
        {
            if (analysisFileInfo == null) return null;

            var result = new StringBuilder();
            foreach (var c in analysisFileInfo.Classes)
            {
                result.Append(Print(c));
            }
            return result.ToString();
        }

        public static string Print(AnalysisClassInfo analysisClassInfo)
        {
            if (analysisClassInfo == null) return null;

            var result = new StringBuilder();

            var analysisFileInfo = analysisClassInfo.FileInfo;
            var fileString = analysisFileInfo != null ? $"{analysisFileInfo.ModuleName}\t{analysisFileInfo.FileName}" : "\t";
            var classString = $"{analysisClassInfo.EntityName}\t{analysisClassInfo.ClassName}\t{analysisClassInfo.FormClassName}\t{analysisClassInfo.ClassStatus}\t{analysisClassInfo.ClassSubStatus}";
            var methods = analysisClassInfo.Methods;
            if (methods.Count == 0) methods = new List<AnalysisMethodInfo>() {
                new AnalysisMethodInfo { 
                    MethodName = analysisClassInfo.ClassName,
                    MethodStatus = analysisClassInfo.ClassStatus,
                    MethodSubStatus = analysisClassInfo.ClassSubStatus,
                    MethodLineCount = 1
                } 
            };

            foreach (var m in methods)
            {
                result.AppendLine($"{fileString}\t{classString}\t{Print(m)}");
            }

            return result.ToString();
        }
        public static string Print(AnalysisMethodInfo analysisMethodInfo)
        {
            if (analysisMethodInfo == null) return null;
            return $"{analysisMethodInfo.MethodName}\t{analysisMethodInfo.MethodStatus}\t{analysisMethodInfo.MethodSubStatus}\t{analysisMethodInfo.MethodLineCount}";
        }
    }

    internal class AnalysisFileInfo
    {
        public string FullPath { get; set; }
        public string ModuleName { get; set; }
        public string FileName { get; set; }
        public List<AnalysisClassInfo> Classes { get; set; }
        public override string ToString()
        {
            return PrintInfo.Print(this);
        }
    }

    internal class AnalysisClassInfo
    {
        public string ClassName { get; set; }
        public string FormClassName { get; set; }
        public string EntityName { get; set; }
        public string ClassStatus { get; set; }
        public string ClassSubStatus { get; set; }
        public AnalysisFileInfo FileInfo { get; set; }
        public List<AnalysisMethodInfo> Methods { get; set; }
        public override string ToString()
        {
            return PrintInfo.Print(this);
        }
    }

    internal class AnalysisMethodInfo
    {
        public string MethodName { get; set; }
        public string MethodStatus { get; set; }
        public string MethodSubStatus { get; set; }
        public int MethodLineCount { get; set; }
        public override string ToString()
        {
            return PrintInfo.Print(this);
        }
    }
}