using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Oetools.Builder.History;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("Compile")]
    public class OeTaskCompile : OeTaskCopy, ITaskCompile {

        public string GetFileCompilationTarget(OeFile file, List<string> fileTaskTargets) {

            return null;
        }
    }
}