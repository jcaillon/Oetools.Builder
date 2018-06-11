using Oetools.Packager.Resources;

namespace Oetools.Packager.Core.Config {
    
    public class ConfigExecutionDatabase {

        public ConfigExecutionDatabase() {
            DatabaseExtractCandoTblType = "T";
            DatabaseExtractCandoTblName = "*";
        }

        public string DatabaseExtractCandoTblType { get; set; }
        public string DatabaseExtractCandoTblName { get; set; }

        public byte[] ProgramDumpTableCrc {
            get { return AblResource.DumpTableCrc; }
        }

        public byte[] ProgramDumpDatabase {
            get { return AblResource.DumpDatabase; }
        }
    }
}