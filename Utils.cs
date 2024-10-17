using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        
        public static Dictionary<uint,string> GetAllPossibleRegisteredFormatNames()
        {
            try
            {
                // Use range 0xC000 to 0xFFFF to get all possible format names in the registered name range
                Dictionary<uint, string> allFormatNames = new Dictionary<uint, string>();
                for (uint i = 0xC000; i <= 0xFFFF; i++)
                {
                    StringBuilder formatName = new StringBuilder(256);
                    if (NativeMethods.GetClipboardFormatName(i, formatName, formatName.Capacity) != 0) // it returns 0 if it fails, so just move on
                    {
                        allFormatNames.Add(i, formatName.ToString());
                    }
                }
                return allFormatNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetAllPossibleRegisteredFormatNames: " + ex.Message);
                MessageBox.Show($"Something went wrong. Error: {ex}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

    } // ----------------- End of class -----------------
} // ----------------- End of namespace -----------------
