using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("CompileZip")]
    public class OeTaskCompileZip : OeTaskZip, ITaskCompile {
    }
}