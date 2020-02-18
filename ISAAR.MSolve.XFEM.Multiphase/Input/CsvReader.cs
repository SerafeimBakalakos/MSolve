using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.XFEM.Multiphase.Input
{
    public class CsvReader
    {
        public double[,] ImportDataFromCSV(string path)
        {
            string[][] dataValues = File.ReadLines(path).Select(x => x.Split(',')).ToArray();
            double[,] dataSet = new double[dataValues.GetLength(0), dataValues[0].Length];

            for (int i = 0; i < dataValues.GetLength(0); i++)
            {
                for (int j = 0; j < dataValues[i].Length; j++)
                {
                    dataSet[i, j] = Convert.ToDouble(dataValues[i][j]);
                }
            }

            return dataSet;
        }
    }
}
