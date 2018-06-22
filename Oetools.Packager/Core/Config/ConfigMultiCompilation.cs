namespace Oetools.Packager.Core.Config {
    
    public class ConfigMultiCompilation {
        
        public IEnvExecutionCompilation Env;
        
        public ConfigMultiCompilation() {
            NumberProcessPerCore = 1;
        }

        public bool ForceSingleProcess { get; set; }
        
        public bool IsDatabaseSingleUser { get; set; }
        
        public bool OnlyGenerateRcode { get; set; }
        
        public int NumberProcessPerCore { get; set; }
        
        /// <summary>
        /// If true, don't actually do anything, just test it
        /// </summary>
        public bool IsTestMode { get; set; }
    }
}
