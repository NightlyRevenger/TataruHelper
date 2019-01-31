// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartyMember.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   PartyMember.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using Sharlayan.Core.Interfaces;

    public class PartyMember : ActorItemBase, IPartyMember {
        public bool IsValid => this.ID > 0 && !string.IsNullOrWhiteSpace(this.Name);

        public PartyMember Clone() {
            var cloned = (PartyMember) this.MemberwiseClone();

            cloned.Coordinate = new Coordinate(this.Coordinate.X, this.Coordinate.Z, this.Coordinate.Y);
            cloned.EnmityItems = new System.Collections.Generic.List<EnmityItem>();
            cloned.StatusItems = new System.Collections.Generic.List<StatusItem>();

            foreach (EnmityItem item in this.EnmityItems) {
                cloned.EnmityItems.Add(
                    new EnmityItem {
                        Enmity = item.Enmity,
                        ID = item.ID,
                        Name = item.Name
                    });
            }

            foreach (StatusItem item in this.StatusItems) {
                cloned.StatusItems.Add(
                    new StatusItem {
                        CasterID = item.CasterID,
                        Duration = item.Duration,
                        IsCompanyAction = item.IsCompanyAction,
                        Stacks = item.Stacks,
                        StatusID = item.StatusID,
                        StatusName = item.StatusName,
                        TargetName = item.TargetName
                    });
            }

            return cloned;
        }
    }
}