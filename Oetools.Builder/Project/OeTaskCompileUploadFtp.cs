using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("CompileUploadFtp")]
    public class OeTaskCompileUploadFtp : OeTaskFtp, ITaskCompile {
    }
}