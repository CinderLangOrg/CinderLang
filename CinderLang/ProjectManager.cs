using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CinderLang
{
    public static class ProjectManager
    {
        public static ProjectFile LoadProject(string proj)
        {
            var doc = XDocument.Load(proj);
            var project = new ProjectFile();

            if (doc.Root == null) ThrowInvalidProjfile();

            var be = doc.Root!.Attribute("backend");
            if (be != null) project.Backend = be.Value;

            var sd = doc.Root!.Element("SourceDir");
            if (sd != null)
            {
                project.SrcDir = sd.Value;

                var sdr = sd.Attribute("recursive");
                if (sdr != null) 
                {
                    if (bool.TryParse(sdr.Value, out var recursive))
                    {
                        if (recursive) project.SearchOpt = SearchOption.AllDirectories;
                        else project.SearchOpt = SearchOption.TopDirectoryOnly;
                    }
                    else ThrowInvalidProjfile();
                }
            }

            var od = doc.Root!.Element("OutputDir");
            if (od != null) project.OutDir = od.Value;

            var dir = Path.GetDirectoryName(Path.GetFullPath(proj));

            project.SrcDir = Path.GetFullPath(Path.Combine(dir!, project.SrcDir));
            project.OutDir = Path.GetFullPath(Path.Combine(dir!, project.OutDir));

            return project;
        }

        static void ThrowInvalidProjfile() => ErrorManager.Throw(ErrorType.Project, $"Invalid project file");
    }

    public class ProjectFile
    {
        public string Backend { get; set; } = "llvm";
        public string SrcDir { get; set; } = "./";
        public string OutDir { get; set; } = "./build";
        public SearchOption SearchOpt { get; set; } = SearchOption.AllDirectories;
    }
}
