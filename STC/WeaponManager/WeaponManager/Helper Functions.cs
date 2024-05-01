using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    class Helper_Functions
    {
        public static string FormatText(float num)
        {
            return num.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        }

        public static string FormatText(double num)
        {
            return num.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        }
    }
}
