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
    public class ToILSharpTranslator : DefaultTranslator
    {
        public ToILSharpTranslator()
        {
            TargetPlatforms = new string[] { };
        }

        string LabelIndentation = "";
        /// <summary>
        /// Gets the string representing the code in Java
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        string GetCodeString(Codom code)
        {
            if (code == null)
                return "";

            string initLabel = "          "; //10 spaces
            string innerIndentation = Indentation + initLabel;

            string lblDelimiter = ":";
            string label = (code.Label != "" ? code.Label + lblDelimiter + initLabel.Substring((code.Label + lblDelimiter).Length) : initLabel);
            string prefixWithLabel = LabelIndentation + label + innerIndentation.Substring((LabelIndentation + label).Length);
            string prefix = LabelIndentation + initLabel + innerIndentation.Substring((LabelIndentation + initLabel).Length);

            string codeString = "";

            if (code is Catch)
            {
                Catch catchCode = (code as Catch);
                if (catchCode.ShowExceptionObject)
                    codeString += prefix + "catch(" + catchCode.CachedCaughtExceptionKind + " " + catchCode.CaughtExceptionObject.ToString(CodomTranslator) + ") {\r\n";
                else
                    codeString += prefix + "catch(" + catchCode.CachedCaughtExceptionKind + ") {\r\n";
                EnterBlock();
                foreach (Codom catchBodyCode in catchCode)
                {
                    codeString += GetCodeString(catchBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
            }
            else if (code is CodeSection)
            {
                CodeSection codeSecCode = (code as CodeSection);
                foreach (Codom codeSecBodyCode in codeSecCode)
                {
                    codeString += GetCodeString(codeSecBodyCode);
                }
            }
            else if (code is Comment)
            {
                Comment commentCode = (code as Comment);
                if (commentCode.IsBlockComment)
                {
                    string finalCommentString = "";
                    string[] commentLines = commentCode.CommentString.Split('\r', '\n');
                    bool isFirstLine = true;

                    foreach (string line in commentLines)
                    {
                        finalCommentString += prefix + (isFirstLine ? "/*" : "  ") + "*" + line + "\r\n";
                        isFirstLine = false;
                    }
                    finalCommentString = finalCommentString.Replace("*/", "* /");
                    finalCommentString += prefix + "  **/\r\n";
                    codeString += finalCommentString;
                }
                else
                {
                    string finalCommentString = "";
                    string[] commentLines = commentCode.CommentString.Split('\r', '\n');
                    foreach (string line in commentLines)
                    {
                        finalCommentString += prefix + "//" + line + "\r\n";
                    }
                    codeString += finalCommentString;
                }
            }
            else if (code is DataSection)
            {
                DataSection dataSecCode = (code as DataSection);
                foreach (Codom dataSecBodyCode in dataSecCode)
                {
                    codeString += GetCodeString(dataSecBodyCode);
                }
            }
            else if (code is Finally)
            {
                Finally finallyCode = (code as Finally);
                codeString += prefix + "finally {\r\n";
                EnterBlock();
                foreach (Codom finallyBodyCode in finallyCode)
                {
                    codeString += GetCodeString(finallyBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
            }
            else if (code is If)
            {
                If ifCode = (code as If);
                codeString += prefixWithLabel + "if (" + GetCodeString(ifCode.Condition) + ")" +
                    ((ifCode.Count == 1 && !(ifCode[0] is Block)) ? "" : " {") + "\r\n"; //always put braces around blocks
                EnterBlock();
                foreach (Codom ifBodyCode in ifCode)
                {
                    codeString += GetCodeString(ifBodyCode);
                }
                ExitBlock();
                codeString += ((ifCode.Count == 1 && !(ifCode[0] is Block)) ? "" : prefix + "}\r\n");
                if (ifCode.HasElse)
                {
                    codeString += prefix + "else" +
                        ((ifCode.Else.Count == 1 && !(ifCode.Else[0] is Block)) ? "" : " {") + "\r\n"; //always put braces around blocks
                    EnterBlock();
                    foreach (Codom ifElseCode in ifCode.Else)
                    {
                        codeString += GetCodeString(ifElseCode);
                    }
                    ExitBlock();
                    codeString += ((ifCode.Else.Count == 1 && !(ifCode.Else[0] is Block)) ? "" : prefix + "}\r\n");
                }
            }
            else if (code is Try)
            {
                Try tryCode = (code as Try);
                codeString += prefix + "try {\r\n";
                EnterBlock();
                foreach (Codom tryBodyCode in tryCode)
                {
                    codeString += GetCodeString(tryBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
            }
            else if (code is Block) //check for this generic block AFTER all the specific blocks
            {
                Block blockCode = (code as Block);
                codeString += prefixWithLabel + "{\r\n";
                EnterBlock();
                foreach (Codom codeSecBodyCode in blockCode)
                {
                    codeString += GetCodeString(codeSecBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
            }
            else //if the code takes one line, simply indent (if not inline), then call the code ToString() then add "\r\n" (if not inline)
            {
                codeString += (code.IsInline ? "" : prefixWithLabel) + code.ToString(CodomTranslator) + (code.IsInline ? "" : "\r\n");
            }

            return codeString;
        }
        /// <summary>
        /// Used to translate single-line codes
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        string CodomTranslator(Codom code)
        {
            if (code is IntDiv)
            {
                IntDiv intDivCode = (code as IntDiv);
                if (intDivCode.IsInline)
                    return (intDivCode.ShowOuterBrackets ? "(" : "") + "(int)(" + GetCodeString(intDivCode.FirstOperand) + " / " + GetCodeString(intDivCode.SecondOperand) + ")" + (intDivCode.ShowOuterBrackets ? ")" : "");
                else
                    return "(int)(" + GetCodeString(intDivCode.FirstOperand) + " / " + GetCodeString(intDivCode.SecondOperand) + ");";
            }
            else
                return null;
        }

        public override void TranslateEvent(Event _event)
        {
            if (_event == null)
                throw new NullReferenceException("Event to translate cannot be null!");

            OnEventTranslating(_event);

            int start = 0;

            try
            {
                if (StartingPoint == TranslationStartingPoints.FromEvent)
                {
                    ResetIndentation();
                    TranslatedString = new StringBuilder(1024);
                }
                start = TranslatedString.Length;

                //write comments--------------------------------------
                string[] comments = _event.GetComments(UseDefaultOnly, TargetPlatforms);
                if (comments.Length > 0)
                {
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "/**"); //\r\n
                    foreach (string comm in comments)
                    {
                        if (comm != null && comm.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "  *" + comm.Trim().Replace("*/", "* /"));
                    }
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "  **/");
                }

                //write annotations---------------------------------------
                string[] annotations = _event.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                foreach (string note in annotations)
                {
                    if (note != null && note.Trim() != "")
                        TranslatedString.AppendLine(Indentation + "[" + note + "]");
                }

                TranslatedString.Append(Indentation);

                //access modifier---------------------------------
                string[] accMods = _event.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                foreach (string accMod in accMods)
                {
                    if (accMod != null && accMod.Trim() != "")
                    {
                        TranslatedString.Append(accMod.Trim() + " ");
                    }
                }

                //attributes-------------------------------------
                string[] attris = _event.GetRawAttributes();//property.GetAttributes(UseDefaultOnly, TargetPlatforms);
                foreach (string attri in attris)
                {
                    if (attri != null && attri.Trim() != "")
                    {
                        TranslatedString.Append(attri.Trim() + " ");
                    }
                }

                //event type-------------------------------------
                string evtTypeStr = "";

                Kind eventKind = _event.GetEventHandlerKind(UseDefaultOnly, TargetPlatforms);
                if (eventKind != null)
                {
                    evtTypeStr = GetAppropriateName(_event.DeclaringKind, eventKind);
                    if (eventKind.IsGenericInstance)
                    {
                        evtTypeStr += eventKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    }
                    else if (eventKind.IsGenericDefinition)
                    {
                        evtTypeStr += eventKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    }
                }
                if (evtTypeStr != null && evtTypeStr.Trim() != "")
                {
                    TranslatedString.Append("event " + evtTypeStr.Trim() + " ");
                }

                //name-------------------------------------
                TranslatedString.AppendLine(_event.GetName(UseDefaultOnly, TargetPlatforms));
                TranslatedString.AppendLine(Indentation + "{");
                EnterBlock();
                if (_event.HasAddMethod)
                {
                    TranslatedString.AppendLine(Indentation + "addon " + _event.AddMethod.GetSignature_CsStyle(UseDefaultOnly, TargetPlatforms) + ";");
                }
                if (_event.HasRemoveMethod)
                {
                    TranslatedString.AppendLine(Indentation + "removeon " + _event.RemoveMethod.GetSignature_CsStyle(UseDefaultOnly, TargetPlatforms) + ";");
                }
                ExitBlock();
                TranslatedString.AppendLine(Indentation + "}");

                OnEventTranslated(_event, TranslatedString.ToString().Substring(start));
            }
            catch (Exception e)
            {
                OnEventTranslated(_event, TranslatedString.ToString().Substring(start), false, e);
            }
        }

        public override void TranslateField(Field field)
        {
            if (field == null)
                throw new NullReferenceException("Field to translate cannot be null!");

            OnFieldTranslating(field);

            int start = 0;

            try
            {
                if (StartingPoint == TranslationStartingPoints.FromField)
                {
                    ResetIndentation();
                    TranslatedString = new StringBuilder(1024);
                }
                start = TranslatedString.Length;

                //write comments--------------------------------------
                string[] comments = field.GetComments(UseDefaultOnly, TargetPlatforms);
                if (comments.Length > 0)
                {
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "/**");
                    foreach (string comm in comments)
                    {
                        if (comm != null && comm.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "  *" + comm.Trim().Replace("*/", "* /"));
                    }
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "  **/");
                }

                //write annotations---------------------------------------
                string[] annotations = field.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                foreach (string note in annotations)
                {
                    if (note != null && note.Trim() != "")
                        TranslatedString.AppendLine(Indentation + "[" + note + "]");
                }

                TranslatedString.Append(Indentation);

                //access modifier---------------------------------
                string[] accMods = field.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                foreach (string accMod in accMods)
                {
                    if (accMod != null && accMod.Trim() != "")
                    {
                        TranslatedString.Append(accMod.Trim() + " ");
                    }
                }

                //attributes-------------------------------------
                string[] attris = field.GetRawAttributes();//field.GetAttributes(UseDefaultOnly, TargetPlatforms);
                foreach (string attri in attris)
                {
                    if (attri != null && attri.Trim() != "")
                    {
                        TranslatedString.Append(attri.Trim() + " ");
                    }
                }

                //field type-------------------------------------
                string fldTypeStr = "";

                Kind fieldKind = field.GetFieldKind(UseDefaultOnly, TargetPlatforms);
                if (fieldKind != null)
                {
                    fldTypeStr = GetAppropriateName(field.DeclaringKind, fieldKind);
                    if (fieldKind.IsGenericInstance)
                    {
                        fldTypeStr += fieldKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    }
                    else if (fieldKind.IsGenericDefinition)
                    {
                        fldTypeStr += fieldKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    }
                }
                if (fldTypeStr != null && fldTypeStr.Trim() != "")
                {
                    TranslatedString.Append(fldTypeStr.Trim() + " ");
                }

                //name-------------------------------------
                TranslatedString.AppendLine(field.GetName(UseDefaultOnly, TargetPlatforms) + ";");

                OnFieldTranslated(field, TranslatedString.ToString().Substring(start));
            }
            catch (Exception e)
            {
                OnFieldTranslated(field, TranslatedString.ToString().Substring(start), false, e);
            }
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

                    if (!kind.UnderlyingType.IsNested)
                    {
                        //write intro to the file-----------------------------------------
                        TranslatedString.AppendLine(Indentation + "/**")
                            .AppendLine(Indentation + " * Author:\t\t\t" + Environment.UserName)
                            .AppendLine(Indentation + " * Generated With:\t" + this.GetType().FullName)
                            .AppendLine(Indentation + " * Date Generated:\t" + DateTime.Now.ToLongDateString() + " @ " + DateTime.Now.ToLongTimeString())
                            .AppendLine(Indentation + " */").AppendLine();

                        //open namespace-----------------------------------------
                        string _namespace = kind.GetNamespace(UseDefaultOnly, TargetPlatforms);
                        if (_namespace != null)
                        {
                            if (_namespace.Trim() != "")
                            {
                                TranslatedString.AppendLine(Indentation + "namespace " + _namespace.Trim());
                                TranslatedString.AppendLine(Indentation + "{");
                                EnterBlock();
                            }
                        }
                    }

                    //write comments--------------------------------------
                    string[] comments = kind.GetComments(UseDefaultOnly, TargetPlatforms);
                    if (comments.Length > 0)
                    {
                        if (comments.Any(i => (i != null && i.Trim() != "")))
                            TranslatedString.AppendLine(Indentation + "/**");
                        foreach (string comm in comments)
                        {
                            if (comm != null && comm.Trim() != "")
                                TranslatedString.AppendLine(Indentation + "  *" + comm.Trim().Replace("*/", "* /"));
                        }
                        if (comments.Any(i => (i != null && i.Trim() != "")))
                            TranslatedString.AppendLine(Indentation + "  **/");
                    }

                    //write annotations---------------------------------------
                    string[] annotations = kind.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "[" + note + "]");
                    }

                    TranslatedString.Append(Indentation);

                    //access modifier---------------------------------
                    string[] accMods = kind.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    foreach (string accMod in accMods)
                    {
                        if (accMod != null && accMod.Trim() != "")
                        {
                            TranslatedString.Append(accMod.Trim() + " ");
                        }
                    }

                    //type attributes-------------------------------------------------
                    string[] attris = kind.GetRawAttributes();//kind.GetAttributes(UseDefaultOnly, TargetPlatforms);
                    foreach (string attri in attris)
                    {
                        if (attri != null && attri.Trim() != "")
                        {
                            TranslatedString.Append(attri.Trim() + " ");
                        }
                    }

                    //name------------------------------------------------------
                    TranslatedString.Append(kind.GetMeaning(UseDefaultOnly, TargetPlatforms) + " " +
                        kind.GetName(UseDefaultOnly, TargetPlatforms));
                    //if (kind.IsGenericInstance) //comment this out as we are ONLY interested in gen params not args
                    //{
                    //    TranslatedString += kind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    //}
                    //else
                    if (kind.IsGenericDefinition)
                    {
                        TranslatedString.Append(kind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                    }

                    //base class--------------------------------------------------
                    dynamic baseKinds = null;
                    if (!kind.IsInterface)
                    {
                        baseKinds = kind.GetBaseKinds(UseDefaultOnly, TargetPlatforms);
                        if (baseKinds != null) //baseKinds can be null for interfaces
                        {
                            Kind baseKind = ((Kind)baseKinds);
                            TranslatedString.Append(" : " + baseKind.GetLongName(UseDefaultOnly, TargetPlatforms));
                            if (baseKind.IsGenericInstance)
                            {
                                TranslatedString.Append(baseKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                            }
                            else if (baseKind.IsGenericDefinition)
                            {
                                TranslatedString.Append(baseKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                            }
                        }
                    }

                    //interfaces--------------------------------------------------
                    dynamic kindInterfaces = kind.GetInterfaces(UseDefaultOnly, TargetPlatforms);
                    if (kindInterfaces != null)
                    {
                        Kind[] interfaces = (Kind[])kindInterfaces;
                        if (interfaces.Length > 0)
                        {
                            if (baseKinds != null && interfaces.Any(i => i != null))
                                TranslatedString.AppendLine(",").Append(Indentation + "\t");

                            string interfaceSec = "";
                            foreach (Kind interf in interfaces)
                            {
                                if (interf != null)
                                {
                                    interfaceSec += interf.GetLongName(UseDefaultOnly, TargetPlatforms);
                                    if (interf.IsGenericInstance)
                                    {
                                        interfaceSec += interf.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                    }
                                    else if (interf.IsGenericDefinition)
                                    {
                                        interfaceSec += interf.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                    }
                                    interfaceSec += ", ";
                                }
                            }
                            interfaceSec = interfaceSec.TrimEnd(' ').TrimEnd(',');
                            TranslatedString.Append(interfaceSec);
                        }
                    }

                    //generic constraints
                    string genConsSec = kind.GetGenericConstraintsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    if (genConsSec != null && genConsSec.Trim() != "")
                    {
                        TranslatedString.AppendLine().Append(Indentation + "\t" + genConsSec);
                    }

                    //enter class block
                    TranslatedString.AppendLine().AppendLine(Indentation + "{");
                    EnterBlock();

                    //write fields------------------------------------------
                    foreach (Field field in kind.GetFields(UseDefaultOnly, TargetPlatforms))
                    {
                        TranslateField(field);
                    }

                    //write methods------------------------------------------
                    foreach (Method method in kind.GetMethods(UseDefaultOnly, TargetPlatforms)) 
                    {
                        TranslateMethod(method);
                    }

                    //write properties------------------------------------------
                    foreach (Property property in kind.GetProperties(UseDefaultOnly, TargetPlatforms))
                    {
                        TranslateProperty(property);
                    }

                    //write events------------------------------------------
                    foreach (Event _event in kind.GetEvents(UseDefaultOnly, TargetPlatforms))
                    {
                        TranslateEvent(_event);
                    }

                    //write nested types------------------------------------------
                    foreach (Kind nestedKind in kind.GetNestedKinds(UseDefaultOnly, TargetPlatforms))
                    {
                        TranslateKind(nestedKind);
                    }

                    //exit class block
                    ExitBlock();
                    TranslatedString.AppendLine(Indentation + "}");

                    if (!kind.UnderlyingType.IsNested)
                    {
                        //close namespace-----------------------------------------
                        string _namespace = kind.GetNamespace(UseDefaultOnly, TargetPlatforms);
                        if (_namespace != null && _namespace.Trim() != "")
                        {
                            ExitBlock();
                            TranslatedString.Append(Indentation + "}");
                        }

                        //create the file------------------------------------------
                        if (CurrentOutputDirectory != null)
                        {
                            string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".il.cs");
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
                                string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".il.cs");
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
            if (method == null)
                throw new NullReferenceException("Method to translate cannot be null!");

            OnMethodTranslating(method);

            int start = 0;

            try
            {
                if (StartingPoint == TranslationStartingPoints.FromMethod)
                {
                    ResetIndentation();
                    TranslatedString = new StringBuilder(1024);
                }
                start = TranslatedString.Length;

                //write comments--------------------------------------
                string[] comments = method.GetComments(UseDefaultOnly, TargetPlatforms);
                if (comments.Length > 0)
                {
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "/**");
                    foreach (string comm in comments)
                    {
                        if (comm != null && comm.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "  *" + comm.Trim().Replace("*/", "* /"));
                    }
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "  **/");
                }

                //write annotations---------------------------------------
                string[] annotations = method.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                if (annotations.Length > 0)
                {
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "[" + note + "]");
                    }
                }

                //method head-------------------------------------
                TranslatedString.Append(Indentation);

                //access modifier-------------------------------------
                string[] accMods = method.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                if (accMods.Length > 0)
                {
                    foreach (string accMod in accMods)
                    {
                        if (accMod != null && accMod.Trim() != "")
                        {
                            TranslatedString.Append(accMod.Trim() + " ");
                        }
                    }
                }

                //attributes-------------------------------------
                string[] attris = method.GetRawAttributes();//method.GetAttributes(UseDefaultOnly, TargetPlatforms);
                if (attris.Length > 0)
                {
                    foreach (string attri in attris)
                    {
                        if (attri != null && attri.Trim() != "")
                        {
                            TranslatedString.Append(attri.Trim() + " ");
                        }
                    }
                }

                //return type-------------------------------------
                if (!method.IsConstructor)
                {
                    string retTypeStr = "";

                    Kind returnKind = (Kind)method.GetReturnKind(UseDefaultOnly, TargetPlatforms);
                    if (returnKind != null)
                    {
                        retTypeStr = GetAppropriateName(method.DeclaringKind, returnKind);
                        if (returnKind.IsGenericInstance)
                        {
                            retTypeStr += returnKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                        }
                        else if (returnKind.IsGenericDefinition)
                        {
                            retTypeStr += returnKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                        }
                    }
                    if (retTypeStr != null && retTypeStr.Trim() != "")
                    {
                        TranslatedString.Append(retTypeStr.Trim() + " ");
                    }
                }

                //name-------------------------------------
                TranslatedString.Append(method.GetName(UseDefaultOnly, TargetPlatforms));
                if (method.IsGenericInstance)
                {
                    TranslatedString.Append(method.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                }
                else if (method.IsGenericDefinition)
                {
                    TranslatedString.Append(method.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                }

                //parameters-------------------------------------
                string paramSection = "";
                Parameter[] paramz = (Parameter[])method.GetParameters(UseDefaultOnly, TargetPlatforms);
                foreach (Parameter param in paramz)
                {
                    Kind paramKindObj = (Kind)param.GetParameterKind(UseDefaultOnly, TargetPlatforms);
                    string paramKindStr = GetAppropriateName(method.DeclaringKind, paramKindObj);
                    //paramKindStr = paramKindStr.Replace("&", "").Replace("*", ""); //remove '*' and '&' for pointers
                    paramKindStr += " ";

                    string paramLongName = paramKindStr + param.GetName(UseDefaultOnly, TargetPlatforms);

                    //optional param
                    if (param.IsOptional && param.HasConstantValue)
                    {
                        paramLongName += " = " + param.ConstantValue;
                    }

                    if (paramLongName != null && paramLongName.Trim() != "")
                    {
                        string paramAttriStr = "";
                        foreach (string paramAttri in param.GetRawAttributes())//param.GetAttributes(UseDefaultOnly, TargetPlatforms))
                        {
                            if (paramAttri != null && paramAttri.Trim() != "")
                            {
                                paramAttriStr += paramAttri + " ";
                            }
                        }
                        if (paramAttriStr.Trim() != "")
                            paramLongName = paramAttriStr + " " + paramLongName;

                        paramSection += paramLongName.Trim() + ", ";
                    }
                }
                paramSection = paramSection.TrimEnd(' ');
                paramSection = paramSection.TrimEnd(',');
                TranslatedString.Append("(" + paramSection + ")");

                //generic constraints
                TranslatedString.Append(" " + method.GetGenericConstraintsSection_CsStyle(UseDefaultOnly, TargetPlatforms));

                //pre-method body---------------------------------
                string preBody = method.GetPreBodySection(UseDefaultOnly, TargetPlatforms);
                if (preBody != null && preBody.Trim() != "")
                    TranslatedString.Append(" " + preBody.Trim());

                //method body-------------------------------------
                Exception exception = null;

                try
                {
                    string methodBodyStr = "";
                    Block methodBody = method.GetBody(CodomTranslator, ReturnPartial, Optimize, UseDefaultOnly, TargetPlatforms);
                    if (methodBody != null)
                    {
                        methodBodyStr += "\r\n" + Indentation + " {\r\n";
                        EnterBlock();
                        LabelIndentation = Indentation;
                        foreach (Codom code in methodBody)
                        {
                            methodBodyStr += GetCodeString(code);
                        }
                        ExitBlock();
                        methodBodyStr += Indentation + "}\r\n";
                        TranslatedString.Append(methodBodyStr);
                    }
                    else
                        TranslatedString.AppendLine(";");
                }
                catch (MethodNotWellFormedException e)
                {
                    exception = e;
                    TranslatedString.AppendLine(";");
                }
                catch (MethodBodyNotReadableException e)
                {
                    exception = e;
                    TranslatedString.AppendLine(";");
                }
                catch (MethodBodyNotWellFormedException e)
                {
                    exception = e;
                    TranslatedString.AppendLine(";");
                }
                catch (Exception e)
                {
                    exception = e;
                    TranslatedString.AppendLine(";");
                }

                OnMethodTranslated(method, TranslatedString.ToString().Substring(start), (exception == null), exception);
            }
            catch (Exception e)
            {
                OnMethodTranslated(method, TranslatedString.ToString().Substring(start), false, e);
            }
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
                CurrentOutputDirectory += "\\" + nameSpace.Trim();
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
            if (property == null)
                throw new NullReferenceException("Property to translate cannot be null!");

            OnPropertyTranslating(property);

            int start = 0;

            try
            {
                if (StartingPoint == TranslationStartingPoints.FromProperty)
                {
                    ResetIndentation();
                    TranslatedString = new StringBuilder(1024);
                }
                start = TranslatedString.Length;

                //write comments--------------------------------------
                string[] comments = property.GetComments(UseDefaultOnly, TargetPlatforms);
                if (comments.Length > 0)
                {
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "/**");
                    foreach (string comm in comments)
                    {
                        if (comm != null && comm.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "  *" + comm.Trim().Replace("*/", "* /"));
                    }
                    if (comments.Any(i => (i != null && i.Trim() != "")))
                        TranslatedString.AppendLine(Indentation + "  **/");
                }

                //write annotations---------------------------------------
                string[] annotations = property.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                if (annotations.Length > 0)
                {
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "[" + note + "]");
                    }
                }

                TranslatedString.Append(Indentation);

                //access modifier---------------------------------
                string[] accMods = property.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                if (accMods.Length > 0)
                {
                    foreach (string accMod in accMods)
                    {
                        if (accMod != null && accMod.Trim() != "")
                        {
                            TranslatedString.Append(accMod.Trim() + " ");
                        }
                    }
                }

                //attributes-------------------------------------
                string[] attris = property.GetRawAttributes();//property.GetAttributes(UseDefaultOnly, TargetPlatforms);
                if (attris.Length > 0)
                {
                    foreach (string attri in attris)
                    {
                        if (attri != null && attri.Trim() != "")
                        {
                            TranslatedString.Append(attri.Trim() + " ");
                        }
                    }
                }

                //property type-------------------------------------
                string propTypeStr = "";

                Kind propKind = property.GetPropertyKind(UseDefaultOnly, TargetPlatforms);
                if (propKind != null)
                {
                    propTypeStr = GetAppropriateName(property.DeclaringKind, propKind);
                    if (propKind.IsGenericInstance)
                    {
                        propTypeStr += propKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    }
                    else if (propKind.IsGenericDefinition)
                    {
                        propTypeStr += propKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                    }
                }
                if (propTypeStr != null && propTypeStr.Trim() != "")
                {
                    TranslatedString.Append("property " + propTypeStr.Trim() + " ");
                }

                //name-------------------------------------
                TranslatedString.AppendLine(property.GetName(UseDefaultOnly, TargetPlatforms));
                TranslatedString.AppendLine(Indentation + "{");
                EnterBlock();
                if (property.HasGetMethod)
                {
                    TranslatedString.AppendLine(Indentation + "get " + property.GetMethod.GetSignature_CsStyle(UseDefaultOnly, TargetPlatforms) + ";");
                }
                if (property.HasSetMethod)
                {
                    TranslatedString.AppendLine(Indentation + "set " + property.SetMethod.GetSignature_CsStyle(UseDefaultOnly, TargetPlatforms) + ";");
                }
                ExitBlock();
                TranslatedString.AppendLine(Indentation + "}");

                OnPropertyTranslated(property, TranslatedString.ToString().Substring(start));
            }
            catch (Exception e)
            {
                OnPropertyTranslated(property, TranslatedString.ToString().Substring(start), false, e);
            }
        }

        public override bool Optimize
        {
            get
            {
                return false;
            }
            set
            {
                //
            }
        }
        public override string[] TargetPlatforms
        {
            get
            {
                return new string[] { };
            }
            set
            {
                //does nothing
            }
        }
        public override bool UseDefaultOnly
        {
            get
            {
                return true;//it is important this remains "true" or the methods in this class will fail
            }
            set
            {
                //do nothing
            }
        }
    }
}
