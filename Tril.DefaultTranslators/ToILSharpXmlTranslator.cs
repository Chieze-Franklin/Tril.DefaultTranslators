using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tril.Codoms;
using Tril.Exceptions;
using Tril.Models;
using Tril.Translators;

namespace Tril.DefaultTranslators
{
    public class ToILSharpXmlTranslator //: DefaultTranslator
    {
        public ToILSharpXmlTranslator() 
        {
            //TargetPlatforms = new string[] { };
        }

        string LabelIndentation = "";
    }
}
