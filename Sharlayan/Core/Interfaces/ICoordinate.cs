// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICoordinate.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ICoordinate.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    public interface ICoordinate {
        double X { get; set; }

        double Y { get; set; }

        double Z { get; set; }

        Coordinate Add(Coordinate coordinate);

        Coordinate Add(float x, float y, float z);

        float AngleTo(Coordinate coordinate);

        float Distance2D(Coordinate coordinate);

        float DistanceTo(Coordinate coordinate);

        Coordinate Normalize();

        Coordinate Normalize(Coordinate origin);

        Coordinate Rotate2D(float angle);

        Coordinate Scale(float scale);

        Coordinate Subtract(Coordinate coordinate);
    }
}