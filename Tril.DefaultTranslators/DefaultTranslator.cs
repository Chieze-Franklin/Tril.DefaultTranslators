using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using Tril.Models;
using Tril.Translators;

namespace Tril.DefaultTranslators
{
    /// <summary>
    /// The base class for the default translators
    /// </summary>
    public abstract class DefaultTranslator : Translator
    {
        protected string CurrentOutputDirectory = "";
        protected StringBuilder TranslatedString = new StringBuilder(1024);

        public DefaultTranslator() 
        {
            TargetPlatforms = new string[] { };
        }

        /// <summary>
        /// Returns the short name or long name of the target kind,
        /// depending on the relationship between the target kind and the calling kind.
        /// </summary>
        /// <param name="callingKind"></param>
        /// <param name="targetKind"></param>
        /// <returns></returns>
        protected string GetAppropriateName(Kind callingKind, Kind targetKind) 
        {
            if (targetKind.IsPlaceHolderGenericParameter)
                return targetKind.GetName(UseDefaultOnly, TargetPlatforms);
            else
            {
                if (targetKind.UnderlyingType.FullName == callingKind.UnderlyingType.FullName)
                    return targetKind.GetName(UseDefaultOnly, TargetPlatforms);
                else if ((callingKind.GetTypeDefinition() != null) && (callingKind.GetTypeDefinition()).NestedTypes
                .Any(t => t.FullName == targetKind.UnderlyingType.FullName))
                    return targetKind.GetName(UseDefaultOnly, TargetPlatforms);
                //else if the targetKind is imported use GetName (not yet implemented)
                //else just return the long name
                else
                    return targetKind.GetLongName(UseDefaultOnly, TargetPlatforms);
            }
        }
        /// <summary>
        /// Accepts an arbitrary string and attempts to return a valid file path from that string
        /// (by removing illegal characters)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        protected string GetValidFilePath(string filePath)
        {
            foreach (char c in Path.GetInvalidPathChars())
            {
                if (c == '<')
                    filePath = filePath.Replace(c.ToString(), "[");
                else if (c == '>')
                    filePath = filePath.Replace(c.ToString(), "]");
                else
                    filePath = filePath.Replace(c.ToString(), "");
            }
            return filePath;
        }

        public override void TranslateBundle(Bundle bundle)
        {
            if (bundle == null)
                throw new NullReferenceException("Bundle to translate cannot be null!");

            OnBundleTranslating(bundle);

            try
            {
                //for each package, write package
                foreach (Package package in bundle.GetPackages(UseDefaultOnly, TargetPlatforms))
                {
                    TranslatePackage(package);
                }
                OnBundleTranslated(bundle);
            }
            catch (Exception e)
            {
                OnBundleTranslated(bundle, null, false, e);
            }
        }
    }
}
