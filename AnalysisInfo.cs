using System.Collections.Generic;
using System.Text;

namespace WebCommentsAnalysis
{
    internal class AnalysisFileInfo
    {
        public string FullPath { get; set; }
        public string ModuleName { get; set; }
        public string FileName { get; set; }
        public List<AnalysisClassInfo> Classes { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();
            var fileString = $"{ModuleName}\t{FileName}";
            foreach (var c in Classes)
            {
                //if (c.EntityName != null) continue; // vypis jen co jsem nenasel
                foreach (var m in c.Methods)
                {
                    result.AppendLine($"{fileString}\t{c.ToString()}\t{m.ToString()}");
                }
            }
            return result.ToString();
        }
    }

    internal class AnalysisClassInfo
    {
        public string ClassName { get; set; }
        public string FormClassName { get; set; }
        public string EntityName { get; set; }
        public string ClassStatus { get; set; }
        public string ClassSubStatus { get; set; }
        public List<AnalysisMethodInfo> Methods { get; set; }

        public override string ToString()
        {
            return $"{EntityName}\t{ClassName}\t{FormClassName}\t{ClassStatus}\t{ClassSubStatus}";
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
            return $"{MethodName}\t{MethodStatus}\t{MethodSubStatus}\t{MethodLineCount}";
        }
    }
}