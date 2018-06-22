using System;
using System.Collections.Generic;

namespace Oetools.Packager.Core.Config {

    public class ConfigDeploymentDifferential : ConfigDeployment {

        /// <summary>
        /// The last deployment, to be able to compute the differences and only compile what is necessary
        /// .........
        /// Optional list of source files (compiled/deployed) of the last deployment, needed
        /// if you want to be able to compute the difference with the current source dir state
        /// Note : the objects in this list WILL BE MODIFIED, make sure to feed a hard copy
        /// </summary>
        public List<FileDeployed> LastDeployedFiles { get; set; }

        /// <summary>
        ///     True if the tool should use a MD5 sum for each file to figure out if it has changed
        /// </summary>
        public bool ComputeMd5 { get; set; }

        /// <summary>
        /// True if all the files should be recompiled/deployed
        /// </summary>
        public bool ForceFullDeploy { get; set; }
    }
}
