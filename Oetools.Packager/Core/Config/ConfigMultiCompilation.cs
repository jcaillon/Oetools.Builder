namespace Oetools.Packager.Core.Config {
    
    public class ConfigMultiCompilation {
        
        public ConfigMultiCompilation() {
            NumberProcessPerCore = 1;
        }

        public bool ForceSingleProcess { get; set; }
        public bool OnlyGenerateRcode { get; set; }
        public int NumberProcessPerCore { get; set; }

    }
}
