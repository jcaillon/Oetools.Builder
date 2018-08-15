using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("CompileProlib")]
    public class OeTaskCompileProlib : OeTaskProlib, ITaskCompile {
    }
}