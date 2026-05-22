using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageCore
{
    public static class SystemLanguage
    {
        public static CultureInfo GetPreferredCulture()
        {
            var c = CultureInfo.InstalledUICulture;
            return c;
        }
    }
}
