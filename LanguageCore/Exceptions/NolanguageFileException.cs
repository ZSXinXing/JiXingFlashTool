using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageCore.Exceptions
{
    public class NolanguageFileException : Exception
    {
        public NolanguageFileException() : base("No default language")
        {
        }
    }
}
