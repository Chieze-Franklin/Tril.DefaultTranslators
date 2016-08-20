using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tril.Models;
using Tril.Translators;

namespace Tril.DefaultTranslators
{
    /*
     * Delete these later.
     * 
     * Declaring Variables:
     *      use "var" in place of the data type.
     * Data Types
     *      undefined, boolean, string, number, object, function
     * Operators:
     *      JS has an unsigned shift right operator (>>>)
     * Functions:
     *      Have to return type
     *      Parameters have no type, not even "var"
     *      No function overloading
     *      function example(arg1, arg2) {...}
     * Objects
     *      built in objects: String, Math, Date, Array
     * Casting
     *      Boolean(object), Number(object), parseInt(string), parseFloat(string), String(object)
     */

    public class ToJavaScriptTranslator : DefaultTranslator
    {
        public ToJavaScriptTranslator()
        {
            TargetPlatforms = new string[] { "js", "*" };
        }

        public override void TranslateEvent(Event _event)
        {
            throw new NotImplementedException();
        }

        public override void TranslateField(Field field)
        {
            throw new NotImplementedException();
        }

        public override void TranslateKind(Kind kind)
        {
            if (kind == null)
                throw new NullReferenceException("Kind to translate cannot be null!");

            OnKindTranslating(kind);

            int start = 0;

            try
            {
                //if (CurrentOutputDirectory != null)
                {
                    if (!kind.UnderlyingType.IsNested || StartingPoint == TranslationStartingPoints.FromKind)
                    {
                        ResetIndentation();
                        TranslatedString = new StringBuilder(1024);
                    }
                    start = TranslatedString.Length;

                    //get meaning----------
                    string kindMeaning = kind.GetMeaning(UseDefaultOnly, TargetPlatforms);
                    //change struct to class
                    if (kindMeaning == "struct")
                        kindMeaning = "class";

                    if (!kind.UnderlyingType.IsNested)
                    {
                        //write intro to the file-----------------------------------------
                        TranslatedString.AppendLine(Indentation + "/**")
                            .AppendLine(Indentation + " * Author:\t\t\t" + Environment.UserName)
                            .AppendLine(Indentation + " * Generated With:\t" + this.GetType().FullName)
                            .AppendLine(Indentation + " * Date Generated:\t" + DateTime.Now.ToLongDateString() + " @ " + DateTime.Now.ToLongTimeString())
                            .AppendLine(Indentation + " */").AppendLine();
                    }

                    if (!kind.UnderlyingType.IsNested)
                    {
                        //create the file------------------------------------------
                        if (CurrentOutputDirectory != null)
                        {
                            string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".js");
                            CreateFile(filePath);
                            WriteFileContentAsString(filePath, TranslatedString.ToString());
                        }
                    }
                }

                //raise event-----------------------------
                OnKindTranslated(kind, TranslatedString.ToString().Substring(start));
            }
            catch (Exception e)
            {
                if (ReturnPartial)
                {
                    try
                    {
                        if (!kind.UnderlyingType.IsNested)
                        {
                            //create the file------------------------------------------
                            if (CurrentOutputDirectory != null)
                            {
                                string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".js");
                                CreateFile(filePath);
                                WriteFileContentAsString(filePath, TranslatedString.ToString());
                            }
                        }
                    }
                    catch { }
                }
                OnKindTranslated(kind, TranslatedString.ToString().Substring(start), false, e);
            }
        }

        public override void TranslateMethod(Method method)
        {
            throw new NotImplementedException();
        }

        public override void TranslatePackage(Package package)
        {
            if (package == null)
                throw new NullReferenceException("Package to translate cannot be null!");

            OnPackageTranslating(package);

            try
            {
                //create all directories
                CurrentOutputDirectory = "";
                string nameSpace = (package.Namespace == null) ? "" : package.Namespace;
                string[] allDirectoryNames = nameSpace.Split('.');
                foreach (string dir in allDirectoryNames)
                {
                    if (dir.Trim() != "")
                    {
                        CurrentOutputDirectory += "\\" + dir.Trim();
                    }
                }
                CreateDirectory(CurrentOutputDirectory);
                //dont clear the directory if there are target types (it means the user wants to just translate those types)
                if (TargetTypes == null || TargetTypes.Count() == 0 || TargetTypes.All(t => t == null))
                    ClearDirectoryContent(CurrentOutputDirectory);

                //for each kind, write kind
                foreach (Kind kind in package.GetKinds())
                {
                    if (TargetTypes == null || TargetTypes.Count() == 0 || TargetTypes.All(t => t == null))
                        TranslateKind(kind);
                    else
                    {
                        if (TargetTypes.Any(t => t == kind.GetLongName(UseDefaultOnly, TargetPlatforms)))
                            TranslateKind(kind);
                    }
                }
                OnPackageTranslated(package);
            }
            catch (Exception e)
            {
                OnPackageTranslated(package, null, false, e);
            }
        }

        public override void TranslateProperty(Property property)
        {
            throw new NotImplementedException();
        }
    }
}
