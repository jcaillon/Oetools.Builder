using System.Collections.Generic;

namespace Oetools.Packager.Core.Config {

    public class ConfigDeployment : ConfigMultiCompilation {
        
        private string _filesPatternCompilable;

        public string FilesPatternCompilable {
            get => !string.IsNullOrEmpty(_filesPatternCompilable) ? _filesPatternCompilable : (_filesPatternCompilable = "*.p,*.w,*.t,*.cls");
            set => _filesPatternCompilable = value;
        }

        
        public string FileDeploymentHook { get; set; }

        public bool ExploreRecursively { get; set; }
        
        private List<DeployRule> _deployRules;

        public List<DeployRule> DeployRules {
            get => _deployRules;
            set => _deployRules = RuleSorter.SortRules(value);
        }

        
    }
}
