// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorItemBase.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ActorItemBase.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using System;
    using System.Collections.Generic;

    using Sharlayan.Core.Enums;
    using Sharlayan.Extensions;

    public class ActorItemBase {
        private string _name;

        public Coordinate Coordinate { get; set; }

        public short CPCurrent { get; set; }

        public short CPMax { get; set; }

        public double CPPercent => safeDivide(this.CPCurrent, this.CPMax);

        public string CPString => $"{this.CPCurrent}/{this.CPMax} [{this.CPPercent:P2}]";

        public List<EnmityItem> EnmityItems { get; protected set; } = new List<EnmityItem>();

        public short GPCurrent { get; set; }

        public short GPMax { get; set; }

        public double GPPercent => safeDivide(this.GPCurrent, this.GPMax);

        public string GPString => $"{this.GPCurrent}/{this.GPMax} [{this.GPPercent:P2}]";

        public float HitBoxRadius { get; set; }

        public int HPCurrent { get; set; }

        public int HPMax { get; set; }

        public double HPPercent => safeDivide(this.HPCurrent, this.HPMax);

        public string HPString => $"{this.HPCurrent}/{this.HPMax} [{this.HPPercent:P2}]";

        public uint ID { get; set; }

        public Actor.Job Job { get; set; }

        public byte JobID { get; set; }

        public byte Level { get; set; }

        public int MPCurrent { get; set; }

        public int MPMax { get; set; }

        public double MPPercent => safeDivide(this.MPCurrent, this.MPMax);

        public string MPString => $"{this.MPCurrent}/{this.MPMax} [{this.MPPercent:P2}]";

        public string Name {
            get => this._name ?? string.Empty;
            set => this._name = value.ToTitleCase();
        }

        public List<StatusItem> StatusItems { get; protected set; } = new List<StatusItem>();

        public int TPCurrent { get; set; }

        public int TPMax { get; set; }

        public double TPPercent => safeDivide(this.TPCurrent, this.TPMax);

        public string TPString => $"{this.TPCurrent}/{this.TPMax} [{this.TPPercent:P2}]";

        public string UUID { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        private double safeDivide(double a, double b)
        {
            try
            {
                if (b == 0)
                    return 0;

                return a / b;
            }
            catch
            {
                // due to multithreading, sometimes b can be set to 0 between the check and the division
                return 0;
            }
        }

        public float GetCastingDistanceTo(ActorItem compare)
        {
            var distance = this.GetHorizontalDistanceTo(compare) - compare.HitBoxRadius - HitBoxRadius;
            return distance > 0
                       ? distance
                       : 0;
        }

        public float GetDistanceTo(ActorItem compare) {
            var distanceX = (float) Math.Abs(compare.X - this.X);
            var distanceY = (float) Math.Abs(compare.Y - this.Y);
            var distanceZ = (float) Math.Abs(compare.Z - this.Z);
            return (float) Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2) + Math.Pow(distanceZ, 2));
        }

        public float GetHorizontalDistanceTo(ActorItem compare) {
            var distanceX = (float) Math.Abs(compare.X - this.X);
            var distanceY = (float) Math.Abs(compare.Y - this.Y);
            return (float) Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));
        }
    }
}