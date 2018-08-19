using System;
using System.Xml.Serialization;

namespace Oetools.Builder.Project {
    
    [Serializable]
    [XmlRoot("Copy")]
    public class OeTaskCopy : OeTaskOnFilesWithTargetFileses {
    }
}