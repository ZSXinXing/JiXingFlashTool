using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageCore.Exceptions
{
    public class NoDefaultLangException : Exception
    {
        public NoDefaultLangException() : base("No language file.")
        {
        }
    }
}
