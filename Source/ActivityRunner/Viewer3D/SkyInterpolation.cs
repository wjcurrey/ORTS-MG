﻿// COPYRIGHT 2009 - 2023 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team.
using Microsoft.Xna.Framework;

namespace Orts.ActivityRunner.Viewer3D
{
    public class SkyInterpolation
    {
        // Size of the sun- and moon-position lookup table arrays.
        // Must be an integral divisor of 1440 (which is the number of minutes in a day).
        public int MaxSteps { get; set; } = 72;
        public double OldClockTime { get; set; }
        public int Step1 { get; set; }
        public int Step2 { get; set; }

        private static float DaylightOffsetS; //=> (Program.DebugViewer == null) ? 0f : (float)Program.DebugViewer.DaylightOffsetHrs * 60 * 60;

        internal (Vector3 solarDirection, Vector3 lunarDirection) SetSunAndMoonDirection(Vector3[] solarPosArray, Vector3[] lunarPosArray, double clockTime)
        {
            // Current solar and lunar position are calculated by interpolation in the lookup arrays.
            // The arrays have intervals of 1200 secs or 20 mins.
            // Using the Lerp() function, so need to calculate the in-between differential
            // The rest of this increments/decrements the array indices and checks for overshoot/undershoot.
            while (clockTime >= OldClockTime - DaylightOffsetS + 1200)
            {
                // Plus key to skip time forwards
                OldClockTime += 1200;
                Step1++;
                Step2++;
                if (Step2 >= MaxSteps)
                {
                    Step2 = 0;
                }

                if (Step1 >= MaxSteps)
                {
                    Step1 = 0;
                }
            }

            if (clockTime <= OldClockTime - DaylightOffsetS)
            {
                // Minus key to skip time backwards
                OldClockTime -= 1200;
                Step1--;
                Step2--;
                if (Step1 < 0)
                {
                    Step1 = MaxSteps - 1;
                }

                if (Step2 < 0)
                {
                    Step2 = MaxSteps - 1;
                }
            }

            float diff = CelestialDiff(clockTime);
            Vector3 solarDirection = new Vector3(MathHelper.Lerp(solarPosArray[Step1].X, solarPosArray[Step2].X, diff),
                MathHelper.Lerp(solarPosArray[Step1].Y, solarPosArray[Step2].Y, diff),
                MathHelper.Lerp(solarPosArray[Step1].Z, solarPosArray[Step2].Z, diff));
            Vector3 lunarDirection = new Vector3(MathHelper.Lerp(lunarPosArray[Step1].X, lunarPosArray[Step2].X, diff),
                MathHelper.Lerp(lunarPosArray[Step1].Y, lunarPosArray[Step2].Y, diff),
                MathHelper.Lerp(lunarPosArray[Step1].Z, lunarPosArray[Step2].Z, diff));

            return (solarDirection, lunarDirection);
        }

        /// <summary>
        /// Returns the advance of time in units of 20 mins (1200 seconds).
        /// Allows for an offset in hours from a control in the DispatchViewer.
        /// This is a user convenience to reveal in daylight what might be hard to see at night.
        /// </summary>
        /// <returns>The advance of time in units of 20 mins (1200 seconds).</returns>
        private float CelestialDiff(double clockTime)
        {
            double diffS = clockTime - (OldClockTime - DaylightOffsetS);
            return (float)diffS / 1200;
        }
    }
}
