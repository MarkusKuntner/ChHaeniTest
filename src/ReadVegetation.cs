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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace GRAL_2001
{
    public partial class ProgramReaders
    {
        //read vegetation
        /// <summary>
        /// Read the vegetation.dat file
        /// </summary>	
        public void ReadVegetation()
        {
            CultureInfo ic = CultureInfo.InvariantCulture;
            
            if (File.Exists("vegetation.dat") == true)
            {
                int block = 0;
                Program.VegetationExist = true;
                Console.WriteLine();
                Console.WriteLine("Reading building file vegetation.dat");

                //last source type closer than distance to a cell
                int sourcetype = -1;
                //last  source number closer than distance to a cell
                int sourcenumber = 0;
                bool isCloser = true;

                try
                {
                    using (StreamReader read = new StreamReader("vegetation.dat"))
                    {
                        double COV = 0;
                        double trunk_height = 0;
                        double crown_height = 0;
                        double trunk_LAD = 0;
                        double crown_LAD = 0;
                        //Vegetation influence
                        
                        string[] text = new string[1];
                        string text1;
                        Int32 IXCUT;
                        Int32 IYCUT;
                        //set marker, that vegetation is available
                        Program.VEG[0][0][0] = 0;

                        while (read.EndOfStream == false)
                        {
                            text1 = read.ReadLine();
                            text = text1.Split(new char[] { '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                            //Vegetation influence
                            if (text[0].Contains("D") && text.Length > 4) // Data line
                            {
                                crown_height = Convert.ToDouble(text[1], ic);
                                trunk_height = Convert.ToDouble(text[2], ic) * crown_height * 0.01;
                                trunk_LAD = Convert.ToDouble(text[3], ic);
                                crown_LAD = Convert.ToDouble(text[4], ic);
                                COV = Convert.ToDouble(text[5], ic) * 0.01;
                            }
                            else if (text.Length > 1 && crown_height > 1) // Coordinates
                            {
                                double cic = Convert.ToDouble(text[0], ic);
                                double cjc = Convert.ToDouble(text[1], ic);
                                IXCUT = (int)((cic - Program.GralWest) / Program.DXK) + 1;
                                IYCUT = (int)((cjc - Program.GralSouth) / Program.DYK) + 1;
                                
                                if ((IXCUT <= Program.NII) && (IXCUT >= 1) && (IYCUT <= Program.NJJ) && (IYCUT >= 1))
                                {
                                    // Filter vegetation cells if they are far away from sources and the SubDomainDistance is activated
                                    if (Program.SubDomainDistance < 10000)
                                    {
                                         (isCloser, sourcetype, sourcenumber) = IsCellCloseToASource(IXCUT, IYCUT, Program.SubDomainDistance, sourcetype, sourcenumber);
                                    }
                                    if (isCloser)
                                    {
                                        block++;
                                        // Create vegetation Array for used cells only -> reduce the memory usage
                                        if (Program.VEG[IXCUT][IYCUT] == null)
                                        {
                                            Program.VEG[IXCUT][IYCUT] = new float [Program.NKK + 1];
                                        }
                                        // use lowest cell 0 because vegetation is zero bound in UVW prognostic microscale calculation
                                        for (int k = Program.KKART[IXCUT][IYCUT] - 1; k < Program.NKK; k++)
                                        {
                                            if (k >= 0)
                                            {
                                                float MeanCellHeight = Program.HOKART[k] - Program.HOKART[Program.KKART[IXCUT][IYCUT]] + Program.DZK[k] * 0.5F;
                                                if (MeanCellHeight < crown_height)
                                                {
                                                    if (MeanCellHeight >= 0 && MeanCellHeight < trunk_height)
                                                    {
                                                        Program.VEG[IXCUT][IYCUT][k] = (float) (0.3 * Math.Pow(COV, 3) * trunk_LAD);
                                                    }
                                                    else if (MeanCellHeight >= 0 && MeanCellHeight >= trunk_height)
                                                    {
                                                        Program.VEG[IXCUT][IYCUT][k] = (float) (0.3 * Math.Pow(COV, 3) * crown_LAD);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //coverage
                                    Program.COV[IXCUT][IYCUT] = (float) COV;
                                    
                                }
                            }
                        }
                    }
                }
                catch
                {
                    string err = "Error when reading file vegetation.dat in line " + block.ToString() + " Execution stopped: press ESC to stop";
                    Console.WriteLine(err);
                    ProgramWriters.LogfileProblemreportWrite(err);

                    if (Program.IOUTPUT <= 0 && Program.WaitForConsoleKey) // not for Soundplan or no keystroke
                    {
                        while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                        {
                            ;
                        }
                    }

                    Environment.Exit(0);
                }
                
                string Info = "Total number of vegetation cells in 2D: " + block.ToString();
                Console.WriteLine(Info);
                ProgramWriters.LogfileGralCoreWrite(Info);
                ProgramWriters.LogfileGralCoreWrite(" ");
            }
            
        }//read vegetation


        /// <summary>
        /// Read the vegetation.dat domain or the vegetation height
        /// </summary>	
        public void ReadVegetationDomain(float[][] VegHeight)
        {
            CultureInfo ic = CultureInfo.InvariantCulture;

            if (File.Exists("vegetation.dat") == true)
            {
                int block = 0;
                Program.VegetationExist = true;
                Console.WriteLine();
                Console.WriteLine("Reading file vegetation.dat");
                //last source type closer than distance to a cell
                int sourcetype = -1;
                //last  source number closer than distance to a cell
                int sourcenumber = 0;
                bool isCloser = true;

                try
                {
                    using (StreamReader read = new StreamReader("vegetation.dat"))
                    {
                        double czo = 0;

                        string[] text = new string[1];
                        string text1;
                        Int32 IXCUT;
                        Int32 IYCUT;
                        while (read.EndOfStream == false)
                        {
                            text1 = read.ReadLine();
                            text = text1.Split(new char[] { '\t', ',' });

                            if (text[0].Contains("D") && text.Length > 4) // Data line
                            {
                                czo = Convert.ToDouble(text[1], ic);
                            }
                            else if (text.Length > 1) // Coordinates
                            {
                                double cic = Convert.ToDouble(text[0], ic);
                                double cjc = Convert.ToDouble(text[1], ic);

                                IXCUT = (int)((cic - Program.GralWest) / Program.DXK) + 1;
                                IYCUT = (int)((cjc - Program.GralSouth) / Program.DYK) + 1;

                                if ((IXCUT <= Program.NII) && (IXCUT >= 1) && (IYCUT <= Program.NJJ) && (IYCUT >= 1))
                                {
                                    // Filter vegetation cells if they are far away from sources and the SubDomainDistance is activated - not for reading vegetation for adaptive roughness lenghts
                                    if (Program.SubDomainDistance < 10000 && VegHeight == null)
                                    {
                                         (isCloser, sourcetype, sourcenumber) = IsCellCloseToASource(IXCUT, IYCUT, Program.SubDomainDistance, sourcetype, sourcenumber);
                                    }
                                    if (isCloser)
                                    {
                                        block++;
                                        if (VegHeight == null)
                                        {
                                            if (czo > 1) // filter low vegetation areas -> they are used for roughness only
                                            {
                                                //compute sub-domains of GRAL for the prognostic wind-field simulations
                                                int iplus = (int)(Math.Min(150, Program.PrognosticSubDomainFactor * czo) / Program.DXK);
                                                int jplus = (int)(Math.Min(150, Program.PrognosticSubDomainFactor * czo) / Program.DYK);
                                                //for (int i1 = IXCUT - iplus; i1 <= IXCUT + iplus; i1++)
                                                Parallel.For(IXCUT - iplus, IXCUT + iplus + 1, Program.pOptions, i1 =>
                                                {
                                                    for (int j1 = IYCUT - jplus; j1 <= IYCUT + jplus; j1++)
                                                    {
                                                        if ((i1 <= Program.NII) && (i1 >= 1) && (j1 <= Program.NJJ) && (j1 >= 1))
                                                        {
                                                            Program.ADVDOM[i1][j1] = 1;
                                                        }
                                                    }
                                                });
                                            }
                                        }
                                        else
                                        {
                                            // read vegetation height
                                            VegHeight[IXCUT][IYCUT] = (float)Math.Max(czo, VegHeight[IXCUT][IYCUT]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    string err = "Error when reading file vegetation.dat in line " + block.ToString() + " Execution stopped: press ESC to stop";
                    Console.WriteLine(err);
                    ProgramWriters.LogfileProblemreportWrite(err);

                    if (Program.IOUTPUT <= 0 && Program.WaitForConsoleKey) // not for Soundplan or no keystroke
                    {
                        while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                        {
                            ;
                        }
                    }

                    Environment.Exit(0);
                }

                string Info = "Total number of vegetation cells in 2D: " + block.ToString();
                Console.WriteLine(Info);
                ProgramWriters.LogfileGralCoreWrite(Info);
                ProgramWriters.LogfileGralCoreWrite(" ");
            }
        }//read vegetation_Domain

        /// <summary>
        /// Read the file VegetationDepoFactor.txt and return the factors or default values 
        /// </summary>	
        public Program.VegetationDepoVel ReadVegetationDeposition()
        {
            CultureInfo ic = CultureInfo.InvariantCulture;
            float gasFact = 1.5F;
            float pmxxFact = 3;

            if (File.Exists("VegetationDepoFactor.txt") == true)
            {
                try
                {
                    string[] text;
                    using (StreamReader read = new StreamReader("VegetationDepoFactor.txt"))
                    {
                        text = read.ReadLine().Split(new char[] { '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        gasFact = Convert.ToSingle(text[0], ic);
                        if (read.EndOfStream == false)
                        {
                            text = read.ReadLine().Split(new char[] { '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            pmxxFact = Convert.ToSingle(text[0], ic);
                        }
                    }
                }
                catch
                {
                }
            }

            Program.VegetationDepoVel depoVel = new Program.VegetationDepoVel(gasFact, pmxxFact);

            if (File.Exists("VegetationDepoFactor.txt") == true)
            {
                string Info = "User defined deposition factors for vegetation areas: " + depoVel.ToString();
                Console.WriteLine(Info);
                ProgramWriters.LogfileGralCoreWrite(Info);
            }

            return depoVel;
        }
    }
}