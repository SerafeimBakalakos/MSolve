using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Utilities
{
    public static class Combinations
    {
        public static IEnumerable<int[]> FindCombosWithLength(int[] options, int comboLength)
        {
            var allCombos = new List<int[]>();
            var currentCombo = new int[comboLength];
            Recurse(options, 0, options.Length - 1, currentCombo, 0, comboLength, allCombos);
            return allCombos;
        }

        public static IEnumerable<int[]> FindAllCombos(int[] options, int minComboLength)
        {
            var allCombos = new List<int[]>();
            for (int comboLength = minComboLength; comboLength <= options.Length; ++comboLength)
            {
                var currentCombo = new int[comboLength];
                Recurse(options, 0, options.Length - 1, currentCombo, 0, comboLength, allCombos);
            }
            return allCombos;
        }

        private static void Recurse(int[] options, int start, int end, int[] currentCombo, int index, int comboLength,
            List<int[]> allCombos)
        {
            // Current combination is ready
            if (index == comboLength)
            {
                var readyCombo = new int[comboLength];
                Array.Copy(currentCombo, readyCombo, comboLength);
                allCombos.Add(readyCombo);
                return;
            }

            // Replace index with all possible elements. The condition "end-i+1 >= comboLength-index" makes sure that 
            // including one element at index will make a combination with remaining elements at remaining positions 
            for (int i = start; (i <= end) && (end - i + 1 >= comboLength - index); ++i)
            {
                currentCombo[index] = options[i];
                Recurse(options, i + 1, end, currentCombo, index + 1, comboLength, allCombos);
            }
        }
    }

    
}
