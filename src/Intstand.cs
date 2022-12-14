#region Copyright
///<remarks>
/// <Graz Lagrangian Particle Dispersion Model>
/// Copyright (C) [2019]  [Dietmar Oettl, Markus Kuntner]
/// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
/// the Free Software Foundation version 3 of the License
/// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
/// You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
///</remarks>
#endregion

using System;
using System.Runtime.CompilerServices;

namespace GRAL_2001
{
    partial class Zeitschleife
    {
        /// <summary>
        ///Vertical interpolation of the horizontal standard deviations of wind component fluctuations
        /// </summary>
        /// <param name="nteil">Particle number</param>
        /// <param name="Roughness">Roughness lenght</param>
        /// <param name="DiffBuilding">Height of particle above ground or buildings</param>
        /// <param name="windge">GRAL wind velocity</param>
        /// <param name="sigmauHurley">Sigma for plume rise</param>
        /// <param name="U0int">Return parameter - standard deviation of u wind fluctuation</param>
        /// <param name="V0int">Return parameter - standard deviation of v wind fluctuation</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float, float) IntStandCalculate(int nteil, float Roughness, float DiffBuilding, float windge, float sigmauHurley)
        {
            // U0 and V0 scaling with wind speed and z0
            float U0int = windge * (0.2F * MathF.Pow(windge, -0.9F) + 0.32F * Roughness + 0.18F);
            U0int = Program.FloatMax(U0int, 0.3F) * Program.StdDeviationV;
            float V0int = U0int;

            if ((Program.IStatistics == Consts.MeteoZR) || (Program.IStatistics == Consts.MeteoSonic)) 
            {
                if (DiffBuilding <= Program.MeasurementHeight[1])
                {
                    U0int *= Program.U0scale[1];
                    V0int *= Program.V0scale[1];
                }
                else if (DiffBuilding >= Program.MeasurementHeight[Program.MetProfileNumb])
                {
                    U0int *= Program.U0scale[Program.MetProfileNumb];
                    V0int *= Program.V0scale[Program.MetProfileNumb];
                }
                else
                {
                    int ipo = 0;
                    for (int iprof = 1; iprof <= Program.MetProfileNumb; iprof++)
                    {
                        if (DiffBuilding > Program.MeasurementHeight[iprof])
                        {
                            ipo = iprof + 1;
                        }
                    }
                    float help = 1 / (Program.MeasurementHeight[ipo] - Program.MeasurementHeight[ipo - 1]) * (DiffBuilding - Program.MeasurementHeight[ipo - 1]);
                    U0int *= Program.U0scale[ipo - 1] + (Program.U0scale[ipo] - Program.U0scale[ipo - 1]) * help;
                    V0int *= Program.V0scale[ipo - 1] + (Program.V0scale[ipo] - Program.V0scale[ipo - 1]) * help;
                }
            }

            //enhancing horizontal diffusion for point sources (Hurley, 2005)
            if (Program.SourceType[nteil] == Consts.SourceTypePoint)
            {
                //U0int += (float)Math.Sqrt(Program.Pow2(sigmauhurly));
                //V0int += (float)Math.Sqrt(Program.Pow2(sigmauhurly));
                U0int += MathF.Abs(sigmauHurley);
                V0int += MathF.Abs(sigmauHurley);
            }
            return (U0int, V0int);
        }

    }
}
