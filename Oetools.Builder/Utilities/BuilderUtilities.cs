#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (BuilderUtilities.cs) is part of Oetools.Builder.
// 
// Oetools.Builder is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Builder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Builder. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using Oetools.Builder.Exceptions;
using Oetools.Builder.Project;
using Oetools.Builder.Utilities.Attributes;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Utilities {
    
    public static class BuilderUtilities {

        /// <summary>
        /// replace all placeholders in variables themselves (read in list order)
        /// we always replace inexisting values by an empty string to avoid problems
        /// </summary>
        /// <param name="variables"></param>
        /// <exception cref="BuildVariableException"></exception>
        public static void ApplyVariablesInVariables(List<OeVariable> variables) {
            foreach (var variable in variables) {
                try {
                    variable.Value = variable.Value.ReplacePlaceHolders(s => {
                        if (s.EqualsCi(variable.Name)) {
                            throw new BuildVariableException(variable, "A variable must not reference itself");
                        }
                        return GetVariableValue(s, variables, string.Empty);
                    });
                } catch (Exception e) {
                    throw new BuildVariableException(variable, $"Variable definition error : {e.Message}");
                }
            }
        }

        /// <summary>
        /// Browse all the string properties of this class (and its children) and replace
        /// placeholders like {{var}} by their variable values
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void ApplyVariablesToProperties<T>(T instance, List<OeVariable> variables) {
            
            // now for each property, we might want to replace the place holders by their values
            // and depending on property attributes, we might want to replace non existing variables by an empty string or leave them as is
            Utils.ForEachPublicPropertyStringInObject(typeof(T), instance, (propInfo, value) => {
                if (string.IsNullOrEmpty(value)) {
                    return value;
                }
                try {
                    var attr = Attribute.GetCustomAttribute(propInfo, typeof(ReplaceVariablesAttribute), true) as ReplaceVariablesAttribute;
                    if (attr == null || !attr.SkipReplace) {
                        return value.ReplacePlaceHolders(s => GetVariableValue(s, variables, attr == null || !attr.LeaveUnknownUntouched ? string.Empty : null));
                    }
                } catch (Exception e) {
                    throw new Exception($"Error replacing variables for {propInfo.GetXmlName()} : {e.Message}", e);
                }
                return value;
            });
        }

        /// <summary>
        /// Returns the value of a variable, read from the environment then from a list of variables
        /// </summary>
        /// <param name="s"></param>
        /// <param name="variables"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private static string GetVariableValue(string s, List<OeVariable> variables, string defaultValue) {
            if (string.IsNullOrEmpty(s)) {
                return string.Empty;
            }
            var varValue = Environment.GetEnvironmentVariable(s);
            if (!string.IsNullOrEmpty(varValue)) {
                return varValue;
            }
            // before-last element or default value
            //return variables?.Where(v => v.Name.EqualsCi(s)).Reverse().ElementAtOrDefault(1)?.Value ?? defaultValue;
            return variables?.LastOrDefault(v => v.Name.EqualsCi(s))?.Value ?? defaultValue;
        }

    }
}