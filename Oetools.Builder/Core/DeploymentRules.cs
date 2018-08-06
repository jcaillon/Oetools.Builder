// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (DeploymentRules.cs) is part of csdeployer.
// 
// csdeployer is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// csdeployer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with csdeployer. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Text;
using Oetools.Utilities.Lib;
using Oetools.Utilities.Lib.Extension;

namespace Oetools.Builder.Core {
    public static class DeploymentRules {
        public static List<DeployRule> GetRules(string confFilePath, out List<Tuple<int, string>> errors) {
            var rulesList = ReadConfigurationFile(confFilePath, out errors);

            // sort the rules
            rulesList.Sort((item1, item2) => {
                // lower step first
                var compare = item1.Step.CompareTo(item2.Step);
                if (compare != 0)
                    return compare;

                var itemTransfer1 = item1 as DeployTransferRule;
                var itemTransfer2 = item2 as DeployTransferRule;

                if (itemTransfer1 != null && itemTransfer2 != null) {
                    // continue first
                    compare = itemTransfer2.ContinueAfterThisRule.CompareTo(itemTransfer1.ContinueAfterThisRule);
                    if (compare != 0)
                        return compare;

                    // copy last
                    compare = itemTransfer1.Type.CompareTo(itemTransfer2.Type);
                    if (compare != 0)
                        return compare;

                    // first line in first in
                    return itemTransfer1.Line.CompareTo(itemTransfer2.Line);
                }

                // filter before transfer
                return itemTransfer1 == null ? 1 : -1;
            });

            return rulesList;
        }

        /// <summary>
        ///     Reads the given rule file
        /// </summary>
        public static List<DeployRule> ReadConfigurationFile(string path, out List<Tuple<int, string>> errors) {
            var returnedErrors = new List<Tuple<int, string>>();

            // get all the rules
            var list = new List<DeployRule>();
            Utils.ForEachLine(path, new byte[0], (lineNb, lineString) => {
                try {
                    var items = lineString.Split('\t');

                    // new variable
                    if (items.Length == 2) {
                        var obj = new DeployVariableRule {
                            Source = path,
                            Line = lineNb + 1,
                            VariableName = items[0].Trim(),
                            Path = items[1].Trim()
                        };

                        if (!obj.VariableName.StartsWith("<") || !obj.VariableName.EndsWith(">")) {
                            returnedErrors.Add(new Tuple<int, string>(lineNb + 1, "Incorrect format for VARIABLE RULE, it should be : <VAR>\tvalue"));
                            return;
                        }

                        if (!string.IsNullOrEmpty(obj.Path))
                            list.Add(obj);
                    }

                    byte step = 0;
                    if (items.Length > 2 && !byte.TryParse(items[0].Trim(), out step))
                        return;

                    // new transfer rule
                    if (items.Length >= 4) {
                        DeployType type;
                        if (Enum.TryParse(items[1].Trim(), true, out type)) {
                            var obj = DeployTransferRule.New(type);
                            obj.Source = path;
                            obj.Line = lineNb + 1;
                            obj.Step = step;
                            obj.ContinueAfterThisRule = items[2].Trim().EqualsCi("yes") || items[2].Trim().EqualsCi("true");
                            obj.SourcePattern = items[3].Trim();

                            var newRules = new List<DeployTransferRule> {obj};
                            if (items.Length > 4) {
                                var multipleTargets = items[4].Split('|');
                                obj.DeployTarget = multipleTargets[0].Trim().Replace('/', '\\');
                                for (var i = 1; i < multipleTargets.Length; i++) {
                                    var copiedRule = obj.GetCopy();
                                    copiedRule.ContinueAfterThisRule = true;
                                    copiedRule.DeployTarget = multipleTargets[i].Trim().Replace('/', '\\');
                                    newRules.Add(copiedRule);
                                }
                            }

                            foreach (var rule in newRules) {
                                rule.ShouldDeployTargetReplaceDollar = rule.DeployTarget.StartsWith(":");
                                if (rule.ShouldDeployTargetReplaceDollar)
                                    rule.DeployTarget = rule.DeployTarget.Remove(0, 1);

                                string errorMsg;
                                var isOk = rule.IsValid(out errorMsg);
                                if (isOk) list.Add(rule);
                                else if (!string.IsNullOrEmpty(errorMsg)) returnedErrors.Add(new Tuple<int, string>(lineNb + 1, errorMsg));
                            }
                        }
                    }

                    if (items.Length == 3) {
                        // new filter rule

                        var obj = new DeployFilterRule {
                            Source = path,
                            Line = lineNb + 1,
                            Step = step,
                            Include = items[1].Trim().EqualsCi("+") || items[1].Trim().EqualsCi("Include"),
                            SourcePattern = items[2].Trim()
                        };
                        obj.RegexSourcePattern = obj.SourcePattern.StartsWith(":") ? obj.SourcePattern.Remove(0, 1) : obj.SourcePattern.Replace('/', '\\').PathWildCardToRegex();

                        if (!string.IsNullOrEmpty(obj.SourcePattern))
                            list.Add(obj);
                    }
                } catch (Exception e) {
                    returnedErrors.Add(new Tuple<int, string>(lineNb + 1, "Syntax error : " + e.Message));
                }
            }, Encoding.Default);

            errors = returnedErrors;

            return list;
        }
    }
}