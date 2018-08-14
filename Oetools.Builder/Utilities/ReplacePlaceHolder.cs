using System;
using Oetools.Utilities.Lib;

namespace Oetools.Builder.Utilities {
    
    /// <summary>
    /// Special attribute that allows to decide wether or not variables should be replaced in a property of type string
    /// and wether or not it should be replaced by an empty string (or left as is) if the variable value is not found
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReplaceVariables : ReplacePlaceHolder {
            
        /// <summary>
        /// Replace unknown values by an empty string
        /// </summary>
        public bool LeaveUnknownUntouched { get; set;  }
    }
}