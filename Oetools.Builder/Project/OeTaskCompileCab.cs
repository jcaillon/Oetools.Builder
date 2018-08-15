using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    [Serializable]
    [XmlRoot("CompileInCab")]
    public class OeTaskCompileCab : OeTaskCab, ITaskCompile {
    }
}