namespace Oetools.Packager.Core.Config {

    public class ConfigDeployment : ConfigMultiCompilation {
        
        private string _filesPatternCompilable;

        public string FilesPatternCompilable {
            get => !string.IsNullOrEmpty(_filesPatternCompilable) ? _filesPatternCompilable : (_filesPatternCompilable = "*.p,*.w,*.t,*.cls");
            set => _filesPatternCompilable = value;
        }

        /// <summary>
        /// True if all the files should be recompiled/deployed
        /// </summary>
        public bool ForceFullDeploy { get; set; }
        
        public string FileDeploymentHook { get; set; }
        
        public bool IsDatabaseSingleUser { get; set; }

        public bool ExploreRecursively { get; set; }
        
        /// <summary>
        /// If true, don't actually do anything, just test it
        /// </summary>
        public bool IsTestMode { get; set; }

    }
}
