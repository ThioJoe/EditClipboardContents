using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditClipboardContents
{
    public static class Utils
    {
        public static bool ListOfArraysNoDuplicates(List<string[]> inputArrayList)
        {
            bool ArrayMatch(string[] x, string[] y)
            {
                if (x == null || y == null || x.Length != 2 || y.Length != 2)
                    return false;
                return x[0] == y[0] && x[1] == y[1];
            }

            for (int i = 0; i < inputArrayList.Count; i++)
            {
                for (int j = i + 1; j < inputArrayList.Count; j++)
                {
                    if (ArrayMatch(inputArrayList[i], inputArrayList[j]))
                        return false;
                }
            }

            return true;
        }
        

    } // ----------------- End of class -----------------
} // ----------------- End of namespace -----------------
