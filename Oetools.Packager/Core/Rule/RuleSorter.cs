#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (RuleSorter.cs) is part of Oetools.Packager.
// 
// Oetools.Packager is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Packager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Packager. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System.Collections.Generic;

namespace Oetools.Packager.Core {
    
    public class RuleSorter {
        
        public static List<DeployRule> SortRules(List<DeployRule> rulesList) {
            
            // sort the rules
            rulesList.Sort((item1, item2) => {
                
                // lower step first
                var compare = item1.Step.CompareTo(item2.Step);
                if (compare != 0) {
                    return compare;
                }

                var itemTransfer1 = item1 as DeployTransferRule;

                if (itemTransfer1 != null && item2 is DeployTransferRule itemTransfer2) {
                    // continue first
                    compare = itemTransfer2.ContinueAfterThisRule.CompareTo(itemTransfer1.ContinueAfterThisRule);
                    if (compare != 0) {
                        return compare;
                    }

                    // copy last
                    compare = itemTransfer1.Type.CompareTo(itemTransfer2.Type);
                    if (compare != 0) {
                        return compare;
                    }

                    // low id first (presumably found earlier than a higher id)
                    return itemTransfer1.Id.CompareTo(itemTransfer2.Id);
                }

                // filter before transfer
                return itemTransfer1 == null ? 1 : -1;
            });

            return rulesList;
        }
    }
}