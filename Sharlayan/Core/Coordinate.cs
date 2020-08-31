// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Coordinate.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Coordinate.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using System;

    using Sharlayan.Core.Interfaces;

    public class Coordinate : ICoordinate {
        public Coordinate(double x = 0.00, double z = 0.00, double y = 0.00) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public Coordinate Add(Coordinate coordinate) {
            return new Coordinate {
                X = this.X + coordinate.X,
                Y = this.Y + coordinate.Y,
                Z = this.Z + coordinate.Z,
            };
        }

        public Coordinate Add(float x, float y, float z) {
            return new Coordinate {
                X = this.X + x,
                Y = this.Y + y,
                Z = this.Z + z,
            };
        }

        public float AngleTo(Coordinate b) {
            Coordinate tmp = b.Subtract(this);
            return (float) Math.Atan2(tmp.X, tmp.Y);
        }

        public float Distance2D(Coordinate coordinate) {
            return (float) Math.Sqrt(Math.Pow(this.X - coordinate.X, 2) + Math.Pow(this.Y - coordinate.Y, 2));
        }

        public float DistanceTo(Coordinate coordinate) {
            return (float) Math.Sqrt(Math.Pow(this.X - coordinate.X, 2) + Math.Pow(this.Y - coordinate.Y, 2) + Math.Pow(this.Z - coordinate.Z, 2));
        }

        public Coordinate Normalize() {
            var length = (float) Math.Sqrt(Math.Pow(this.X, 2) + Math.Pow(this.Y, 2) + Math.Pow(this.Z, 2));
            return new Coordinate {
                X = this.X / length,
                Y = this.Y / length,
                Z = this.Z / length,
            };
        }

        public Coordinate Normalize(Coordinate origin) {
            Coordinate coordinate = this.Subtract(origin);
            return coordinate.Normalize();
        }

        public Coordinate Rotate2D(float angle) {
            return new Coordinate {
                X = (float) (this.X * Math.Cos(angle) - this.Y * Math.Sin(angle)),
                Y = (float) (this.Y * Math.Cos(angle) + this.X * Math.Sin(angle)),
                Z = this.Z,
            };
        }

        public Coordinate Scale(float scale) {
            return new Coordinate {
                X = this.X * scale,
                Y = this.Y * scale,
                Z = this.Z * scale,
            };
        }

        public Coordinate Subtract(Coordinate coordinate) {
            return new Coordinate {
                X = this.X - coordinate.X,
                Y = this.Y - coordinate.Y,
                Z = this.Z - coordinate.Z,
            };
        }

        public override string ToString() {
            return this.X + ", " + this.Y + ", " + this.Z;
        }
    }
}