using Oetools.Packager.Resources.Openedge;

namespace Oetools.Packager.Core.Config {
    
    public class ConfigExecutionDatabase {

        public ConfigExecutionDatabase() {
            DatabaseExtractCandoTblType = "T";
            DatabaseExtractCandoTblName = "*";
        }

        public string DatabaseExtractCandoTblType { get; set; }
        public string DatabaseExtractCandoTblName { get; set; }

        public string ProgramDumpTableCrc => OpenedgeResources.GetOpenedgeAsStringFromResources(@"DumpTableCrc.p");

        public string ProgramDumpDatabase => OpenedgeResources.GetOpenedgeAsStringFromResources(@"DumpDatabase.p");
    }
}