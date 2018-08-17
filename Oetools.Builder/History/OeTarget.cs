using System.Xml.Serialization;
using Oetools.Builder.Utilities;

namespace Oetools.Builder.History {
    public abstract class OeTarget {

        public virtual string GetTargetFilePath() => null;

    }
}