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
    public class ToJavaTranslator : DefaultTranslator
    {
        public ToJavaTranslator() 
        {
            TargetPlatforms = new string[] { "java", "*" };
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
                //if (catchCode.ShowExceptionObject)
                codeString += prefix + "catch(" + catchCode.CachedCaughtExceptionKind + " " + catchCode.CaughtExceptionObject.ToString(CodomTranslator) + ") {\r\n";
                //else
                //    codeString += prefix + "catch(" + catchCode.CachedCaughtExceptionKind + ") {\r\n";
                EnterBlock();
                foreach (Codom catchBodyCode in catchCode)
                {
                    codeString += GetCodeString(catchBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
            }
            //else if (code is CheckFloatingPointFinite) { }
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
                    if (ifCode.Else.Count == 1 && ifCode.Else[0] is If && ifCode.Else[0].Label == "")
                    {
                        codeString += prefix + "else " + GetCodeString(ifCode.Else[0]).TrimStart();
                    }
                    else
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
            if (code is AddressOf) 
            {
                AddressOf addressOfCode = (code as AddressOf);
                if (addressOfCode.IsInline)
                    return GetCodeString(addressOfCode.Operand);
                return GetCodeString(addressOfCode.Operand) + ";";
            }
            else if (code is As)
            {
                As asCode = (code as As);
                if (asCode.IsInline)
                    return (asCode.ShowOuterBrackets ? "(" : "") + "(" + asCode.CachedKind + ")" + GetCodeString(asCode.ObjectToConvert) + (asCode.ShowOuterBrackets ? ")" : "");
                return "(" + asCode.CachedKind + ")" + GetCodeString(asCode.ObjectToConvert) + ";";
            }
            else if (code is BaseParamRef)
            {
                BaseParamRef baseCode = (code as BaseParamRef);
                if (baseCode.IsInline)
                    return "super";
                return "super" + ";";
            }
            else if (code is Checked) 
            {
                Checked checkedCode = (code as Checked);
                if (checkedCode.IsInline)
                    return GetCodeString(checkedCode.Operand);
                return GetCodeString(checkedCode.Operand) + ";";
            }
            else if (code is Compare)
            {
                Compare compCode = (code as Compare);
                if (((compCode.FirstOperand is As && compCode.SecondOperand is Null) ||
                    (compCode.FirstOperand is Null && compCode.SecondOperand is As)) && compCode.Comparison != Compare.Comparisons.Equal)
                {
                    if (compCode.IsInline)
                        return (compCode.ShowOuterBrackets ? "(" : "") + GetCodeString(compCode.FirstOperand) + " != " +
                            GetCodeString(compCode.SecondOperand) + (compCode.ShowOuterBrackets ? ")" : "");
                    return GetCodeString(compCode.FirstOperand) + " != " + GetCodeString(compCode.SecondOperand) + ";";
                }
                else
                    return null;
            }
            else if (code is DataConversion) 
            {
                DataConversion dataConvCode = (code as DataConversion);
                if (dataConvCode.Conversion == DataConversion.Conversions.ToNativeInt ||
                    dataConvCode.Conversion == DataConversion.Conversions.ToUNativeInt)
                {
                    string conv = "int";// "native int";
                    if (dataConvCode.IsInline)
                        return (dataConvCode.ShowOuterBrackets ? "(" : "") + "(" + conv + ")" + GetCodeString(dataConvCode.Operand) + (dataConvCode.ShowOuterBrackets ? ")" : "");
                    return "(" + conv + ")" + GetCodeString(dataConvCode.Operand) + ";";
                }
                else
                    return null;
            }
            else if (code is DecimalConst)
            {
                DecimalConst decimalCode = (code as DecimalConst);
                return decimalCode.Value.ToString() + "D";
            }
            else if (code is Default) 
            {
                Default defaultCode = (code as Default);
                Kind kind = defaultCode.GetKind();
                if (kind.UnderlyingType.IsValueType)// && kind.GetTypeDefinition() != null)
                {
                    return "new " + defaultCode.CachedKind + "()";
                }
                else
                {
                    return "null";
                }
            }
            else if (code is DoubleConst)
            {
                DoubleConst doubleCode = (code as DoubleConst);
                return doubleCode.Value.ToString() + "D";
            }
            else if (code is DynamicArglist)
            {
                DynamicArglist arglistCode = (code as DynamicArglist);
                if (arglistCode.IsInline)
                    return "new " + arglistCode.GetKind().GetLongName(UseDefaultOnly, TargetPlatforms) + "()";
                return "new " + arglistCode.GetKind().GetLongName(UseDefaultOnly, TargetPlatforms) + "();";
            }
            else if (code is DynamicMakeRef)
            {
                DynamicMakeRef makerefCode = (code as DynamicMakeRef);
                if (makerefCode.IsInline)
                    return "System.TypedReference.MakeTypedReference(" + GetCodeString(makerefCode.Pointer) + ", null)";
                return "System.TypedReference.MakeTypedReference(" + GetCodeString(makerefCode.Pointer) + ", null);";
            }
            else if (code is DynamicRefType)
            {
                DynamicRefType reftypeCode = (code as DynamicRefType);
                if (reftypeCode.IsInline)
                    return "System.TypedReference.TargetTypeToken(" + GetCodeString(reftypeCode.TypedReference) + ")";
                return "System.TypedReference.TargetTypeToken(" + GetCodeString(reftypeCode.TypedReference) + ");";
            }
            else if (code is DynamicRefValue)
            {
                DynamicRefValue refvalueCode = (code as DynamicRefValue);
                if (refvalueCode.IsInline)
                    return "(" + refvalueCode.TargetKind.GetLongName(UseDefaultOnly, TargetPlatforms) + ")System.TypedReference.ToObject(" + GetCodeString(refvalueCode.TypedReference) + ")";
                return "(" + refvalueCode.TargetKind.GetLongName(UseDefaultOnly, TargetPlatforms) + ")System.TypedReference.ToObject(" + GetCodeString(refvalueCode.TypedReference) + ");";
            }
            //else if (code is Goto || code is Leave) { }***************************************************************
            else if (code is IndirectAssignment)
            {
                IntDiv intDivCode = (code as IntDiv);
                if (intDivCode.IsInline)
                    return (intDivCode.ShowOuterBrackets ? "(" : "") + GetCodeString(intDivCode.FirstOperand) + " = " + GetCodeString(intDivCode.SecondOperand) + (intDivCode.ShowOuterBrackets ? ")" : "");
                else
                    return GetCodeString(intDivCode.FirstOperand) + " = " + GetCodeString(intDivCode.SecondOperand) + ";";
            }
            else if (code is IntDiv)
            {
                IntDiv intDivCode = (code as IntDiv);
                if (intDivCode.IsInline)
                    return (intDivCode.ShowOuterBrackets ? "(" : "") + "(int)(" + GetCodeString(intDivCode.FirstOperand) + " / " + GetCodeString(intDivCode.SecondOperand) + ")" + (intDivCode.ShowOuterBrackets ? ")" : "");
                else
                    return "(int)(" + GetCodeString(intDivCode.FirstOperand) + " / " + GetCodeString(intDivCode.SecondOperand) + ");";
            }
            else if (code is LengthOf)
            {
                LengthOf lengthCode = (code as LengthOf);
                if (lengthCode.IsInline)
                    return (lengthCode.ShowOuterBrackets ? "(" : "") + GetCodeString(lengthCode.Operand) + ".length" + (lengthCode.ShowOuterBrackets ? ")" : "");
                else
                    return GetCodeString(lengthCode.Operand) + ".length;";
            }
            //else if (code is LocAlloc)
            else if (code is PointerElement)
            {
                PointerElement pointerCode = (code as PointerElement);
                if (pointerCode.IsInline)
                {
                    return GetCodeString(pointerCode.Pointer);
                }
                return GetCodeString(pointerCode.Pointer) + ";";
            }
            else if (code is ShiftRight)
            {
                ShiftRight shrCode = (code as ShiftRight);
                string op = " >> ";
                if (shrCode.IsUnsigned)
                    op = " >>> ";
                if (shrCode.IsInline)
                    return (shrCode.ShowOuterBrackets ? "(" : "") + GetCodeString(shrCode.FirstOperand) + op + GetCodeString(shrCode.SecondOperand) + (shrCode.ShowOuterBrackets ? ")" : "");
                else
                    return GetCodeString(shrCode.FirstOperand) + op + GetCodeString(shrCode.SecondOperand) + ";";
            }
            //else if (code is SizeOf) { }
            //else if (code is Throw) { }how do you handle throws with no value to throw
            else if (code is UIntConst)
            {
                UIntConst uIntCode = (code as UIntConst);
                return uIntCode.Value.ToString();
            }
            else if (code is ULongConst)
            {
                ULongConst uLongCode = (code as ULongConst);
                return uLongCode.Value.ToString() + "L";
            }
            else
                return null;
        }

        /// <summary>
        /// Gets a string that represents the generic arguments or parameters section of the Tril Model.
        /// For instance, for a C# type, the generic section starts with "&lt;",
        /// then comma-separated list of all the generic arguments/parameters, then a ">".
        /// E.g. &lt;T1, T2, T3>
        /// For java, each generic parameter is followed by its constraint; 
        /// E.g. &lt;T1 extends IComparable&lt;T1>, T2, T3>
        /// </summary>
        /// <param name="callingKind">
        /// The kind being translated when this method was called, 
        /// or the kind that declares the method being translated when this method was called.
        /// </param>
        /// <param name="genericObjs"></param>
        /// <returns></returns>
        protected string GetGenericArgsOrParamsSection(Kind callingKind, Kind[] genericObjs)
        {
            string paramSec = "";

            //generic params------------------------------------------------------
            if (genericObjs.Length > 0)
            {
                paramSec = "<";
                foreach (Kind genericObj in genericObjs)
                {
                    paramSec += GetAppropriateName(callingKind, genericObj);
                    if (genericObj.IsGenericInstance)
                    {
                        Kind[] genericObjArgs = genericObj.GetGenericArguments();
                        if (genericObjArgs.Length > 0)
                        {
                            paramSec += GetGenericArgsOrParamsSection(callingKind, genericObjArgs);
                        }
                    }
                    else if (genericObj.IsGenericDefinition)
                    {
                        Kind[] genericObjParams = genericObj.GetGenericParameters();
                        if (genericObjParams.Length > 0)
                        {
                            paramSec += GetGenericArgsOrParamsSection(callingKind, genericObjParams);
                        }
                    }

                    if (genericObj.IsGenericParameter)
                    {
                        Kind[] genericParamsCons = genericObj.GetGenericConstraints();
                        if (genericParamsCons.Length > 0)
                        {
                            if (genericParamsCons[0].IsInterface)
                                paramSec += " implements ";
                            else
                                paramSec += " extends ";
                            for (int index = 0; index < genericParamsCons.Length; index++)
                            {
                                Kind constraint = genericParamsCons[index];
                                paramSec += GetAppropriateName(callingKind, constraint);
                                if (constraint.IsGenericInstance)
                                {
                                    Kind[] constraintArgs = constraint.GetGenericArguments();
                                    if (constraintArgs.Length > 0)
                                    {
                                        paramSec += GetGenericArgsOrParamsSection(callingKind, constraintArgs);
                                    }
                                }
                                else if (constraint.IsGenericDefinition)
                                {
                                    Kind[] constraintParams = constraint.GetGenericParameters();
                                    if (constraintParams.Length > 0)
                                    {
                                        paramSec += GetGenericArgsOrParamsSection(callingKind, constraintParams);
                                    }
                                }
                                paramSec += ", ";
                            }
                            paramSec = paramSec.TrimEnd(' ');
                            paramSec = paramSec.TrimEnd(',');
                        }
                    }

                    paramSec += ", ";
                }
                paramSec = paramSec.TrimEnd(' ');
                paramSec = paramSec.TrimEnd(',');
                paramSec += ">";
            }

            return paramSec;
        }

        public override void TranslateEvent(Event _event)
        {
            throw new NotSupportedException("Java does not support events!");
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

                string currentMeaning = field.DeclaringKind.GetMeaning(UseDefaultOnly, TargetPlatforms);
                //change struct to class
                if (currentMeaning == "struct")
                    currentMeaning = "class";

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
                //write only user-defined annotations
                if (field.HasUserDefinedAnnotations(TargetPlatforms))
                {
                    string[] annotations = field.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "@" + note);
                    }
                }

                if (currentMeaning == "enum")// && decKindStr != null && decKindStr.Trim() != "" && decKindStr.Trim() == fldTypeStr.Trim())
                {
                    //if fldTypeStr == the name of the declaring kind, dont show it
                    string fldTypeStr = "";

                    if (field.HasUserDefinedFieldKind(TargetPlatforms)) //this means the field type would be returned as a string
                    {
                        fldTypeStr = field.GetFieldKind(UseDefaultOnly, TargetPlatforms);
                    }
                    else //this means the field type would be returned as a Tril.Kind
                    {
                        Kind fieldKind = field.GetFieldKind(UseDefaultOnly, TargetPlatforms);
                        if (fieldKind != null)
                        {
                            fldTypeStr = fieldKind.GetLongName(UseDefaultOnly, TargetPlatforms);
                        }
                    }
                    if (field.DeclaringKind.GetLongName(UseDefaultOnly, TargetPlatforms) == fldTypeStr.Trim())
                    {
                        //name-------------------------------------
                        TranslatedString.AppendLine(Indentation + field.GetName(UseDefaultOnly, TargetPlatforms) + ",");
                    }
                }
                else
                {
                    TranslatedString.Append(Indentation);
                    if (!field.HasUserDefinedAttributes(TargetPlatforms))
                        if (field.GetFieldDefinition() != null && field.GetFieldDefinition().IsNotSerialized)
                            TranslatedString.Append("transient ");

                    //access modifier---------------------------------
                    string[] accMods = field.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    foreach (string accMod in accMods)
                    {
                        if (accMod != null && accMod.Trim() != "")
                        {
                            if (accMod.Trim() == "internal" || accMod.Trim() == "protected internal")
                                TranslatedString.Append("protected ");
                            else
                                TranslatedString.Append(accMod.Trim() + " ");
                            break; //this break is because this lang expects only one access modifier on a method
                        }
                    }

                    //attributes-------------------------------------
                    string[] attris = field.GetAttributes(UseDefaultOnly, TargetPlatforms);
                    foreach (string attri in attris)
                    {
                        if (attri != null && attri.Trim() != "")
                        {
                            //change readonly to final
                            if (attri.Trim() == "readonly")
                                TranslatedString.Append("final ");
                            //change const/"static readonly" to "static final"
                            else if (attri.Trim() == "const" || attri.Trim() == "static readonly")
                                TranslatedString.Append("static final ");
                            else
                                TranslatedString.Append(attri.Trim() + " ");
                        }
                    }

                    //field type-------------------------------------
                    string fldTypeStr = "";

                    if (field.HasUserDefinedFieldKind(TargetPlatforms)) //this means the field type would be returned as a string
                    {
                        fldTypeStr = field.GetFieldKind(UseDefaultOnly, TargetPlatforms);
                    }
                    else //this means the field type would be returned as a Tril.Kind
                    {
                        Kind fieldKind = field.GetFieldKind(UseDefaultOnly, TargetPlatforms);
                        if (fieldKind != null)
                        {
                            fldTypeStr = GetAppropriateName(field.DeclaringKind, fieldKind);
                            if (fieldKind.IsGenericInstance)
                            {
                                Kind[] genericArgs = fieldKind.GetGenericArguments();
                                if (genericArgs.Length > 0)
                                {
                                    fldTypeStr += GetGenericArgsOrParamsSection(field.DeclaringKind, genericArgs);
                                }
                            }
                            else if (fieldKind.IsGenericDefinition)
                            {
                                //generic params section------------------------------------------------------
                                if (fieldKind.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                                {
                                    fldTypeStr += fieldKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                }
                                else //this means Tril.Kind[] is returned
                                {
                                    Kind[] genericParams = fieldKind.GetGenericParameters();
                                    if (genericParams.Length > 0)
                                    {
                                        fldTypeStr += GetGenericArgsOrParamsSection(field.DeclaringKind, genericParams);
                                    }
                                }
                            }
                        }
                    }
                    if (fldTypeStr != null && fldTypeStr.Trim() != "")
                    {
                        TranslatedString.Append(fldTypeStr.Trim() + " ");
                    }

                    //name-------------------------------------
                    TranslatedString.AppendLine(field.GetName(UseDefaultOnly, TargetPlatforms) + ";");
                }

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

                        //write the package-----------------------------------------
                        string _namespace = kind.GetNamespace(UseDefaultOnly, TargetPlatforms);
                        if (_namespace != null)
                        {
                            if (_namespace.Trim() != "")
                                TranslatedString.AppendLine(Indentation + "package " + _namespace.Trim() + ";").AppendLine();
                        }

                        //write imported packages-----------------------------------------
                        string[] imports = kind.GetImports(UseDefaultOnly, TargetPlatforms);
                        if (imports.Length > 0)
                        {
                            foreach (string import in imports)
                            {
                                if (import != null && import.Trim() != "")
                                    TranslatedString.AppendLine(Indentation + "import " + import.Trim() + ";");
                            }
                            if (imports.Any(i => (i != null && i.Trim() != "")))
                                TranslatedString.AppendLine();
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
                    //write only user-defined annotations
                    if (kind.HasUserDefinedAnnotations(TargetPlatforms))
                    {
                        string[] annotations = kind.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                        if (annotations.Length > 0)
                        {
                            foreach (string note in annotations)
                            {
                                if (note != null && note.Trim() != "")
                                    TranslatedString.AppendLine(Indentation + "@" + note);
                            }
                        }
                    }

                    //access modifiers-------------------------------------------------
                    TranslatedString.Append(Indentation);
                    string[] accMods = kind.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    if (accMods.Length > 0)
                    {
                        foreach (string accMod in accMods)
                        {
                            if (accMod != null && accMod.Trim() != "")
                            {
                                if (accMod.Trim() == "internal" || accMod.Trim() == "protected internal")
                                    TranslatedString.Append("protected ");
                                else
                                    TranslatedString.Append(accMod.Trim() + " ");
                                break; //this break is because this language expects only one access modifier here
                            }
                        }
                    }

                    //type attributes-------------------------------------------------
                    if (kindMeaning == "class")
                    {
                        string[] attris = kind.GetAttributes(UseDefaultOnly, TargetPlatforms);
                        if (attris.Length > 0)
                        {
                            foreach (string attri in attris)
                            {
                                if (attri != null && attri.Trim() != "")
                                {
                                    if (attri.Trim() == "sealed")
                                        TranslatedString.Append("final ");
                                    else
                                        TranslatedString.Append(attri.Trim() + " ");
                                }
                            }
                        }
                    }

                    //name------------------------------------------------------
                    TranslatedString.Append(kindMeaning + " " + kind.GetName(UseDefaultOnly, TargetPlatforms));
                    //if (kind.IsGenericInstance) //comment this out as we are ONLY interested in gen params not args
                    //{
                    //Kind[] genericArgs = kind.GetGenericArguments();
                    //if (genericArgs.Length > 0)
                    //{
                    //    TranslatedString += GetGenericArgsOrParamsSection(kind, genericArgs);
                    //}
                    //}
                    //else
                    if (kind.IsGenericDefinition)
                    {
                        //generic params section------------------------------------------------------
                        if (kind.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                        {
                            TranslatedString.Append(kind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                        }
                        else //this means Tril.Kind[] is returned
                        {
                            Kind[] genericParams = kind.GetGenericParameters();
                            if (genericParams.Length > 0)
                            {
                                TranslatedString.Append(GetGenericArgsOrParamsSection(kind, genericParams));
                            }
                        }
                    }

                    //base class--------------------------------------------------
                    if (kindMeaning == "class")// || kindMeaning == "enum")
                    {
                        dynamic baseKinds = kind.GetBaseKinds(UseDefaultOnly, TargetPlatforms);
                        if (baseKinds != null) //baseKinds can be null for interfaces
                        {
                            if (kind.HasUserDefinedBaseKinds(TargetPlatforms)) //this means kind.GetBaseKinds returns a string[]
                            {
                                string[] baseKindsArray = (string[])baseKinds;
                                if (baseKindsArray.Length == 1 && baseKindsArray[0] != null)
                                    if ((baseKindsArray[0]).Trim() != "")
                                        TranslatedString.Append(" extends " + (baseKindsArray[0]).Trim());
                            }
                            else //this means kind.BaseKind returns a single Tril.Kind
                            {
                                Kind baseKind = ((Kind)baseKinds);
                                string genericParamsSec = "";
                                if (baseKind.IsGenericInstance)
                                {
                                    Kind[] genericArgs = baseKind.GetGenericArguments();
                                    if (genericArgs.Length > 0)
                                    {
                                        genericParamsSec += GetGenericArgsOrParamsSection(kind, genericArgs);
                                    }
                                }
                                else if (baseKind.IsGenericDefinition)
                                {
                                    //generic params section------------------------------------------------------
                                    if (baseKind.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                                    {
                                        genericParamsSec += baseKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                    }
                                    else //this means Tril.Kind[] is returned
                                    {
                                        Kind[] genericParams = baseKind.GetGenericParameters();
                                        if (genericParams.Length > 0)
                                        {
                                            genericParamsSec += GetGenericArgsOrParamsSection(kind, genericParams);
                                        }
                                    }
                                }
                                TranslatedString.Append(" extends " + baseKind.GetLongName(UseDefaultOnly, TargetPlatforms) + genericParamsSec);
                            }
                        }
                    }

                    //interfaces--------------------------------------------------
                    dynamic kindInterfaces = kind.GetInterfaces(UseDefaultOnly, TargetPlatforms);
                    if (kindInterfaces != null)
                    {
                        if (kind.HasUserDefinedInterfaces(TargetPlatforms)) //this means kind.GetInterfaces() returns a string[]
                        {
                            string[] interfaces = (string[])kindInterfaces;
                            if (interfaces.Length > 0)
                            {
                                if (interfaces.Any(i => (i != null && i.Trim() != "")))
                                    TranslatedString.AppendLine().Append(Indentation + "\timplements ");

                                #region NEW METHOD of concatenating interfaces
                                var allNonNullAndNonEmptyInterfs = interfaces.SkipWhile(i => i == null || i.Trim() == "");
                                //OR
                                //allNonNullAndNonEmptyInterfs = interfaces.TakeWhile(i => i != null && i.Trim() != "");
                                //OR
                                //allNonNullAndNonEmptyInterfs = interfaces.Where<string>(i => i != null && i.Trim() != "");
                                string joinedInterfs = string.Join(", ", allNonNullAndNonEmptyInterfs);//interfaces);
                                TranslatedString.Append(joinedInterfs);
                                #endregion

                                #region OLD METHOD of concatenating interfaces
                                //foreach (string interf in interfaces)
                                //{
                                //    if (interf != null && interf.Trim() != "")
                                //        TranslatedString.Append(interf.Trim() + ", ");
                                //}
                                //string transString = TranslatedString.ToString().TrimEnd(' ').TrimEnd(',');
                                //TranslatedString = new StringBuilder(transString);
                                #endregion
                            }
                        }
                        else if (kindMeaning != "enum") //this means kind.GetInterfaces() returns a Kind[]
                        {
                            Kind[] interfaces = (Kind[])kindInterfaces;
                            if (interfaces.Length > 0)
                            {
                                if (interfaces.Any(i => i != null))
                                    TranslatedString.AppendLine().Append(Indentation + "\timplements ");

                                #region NEW METHOD of concatenating interfaces
                                var allNonNullInterfs = interfaces.SkipWhile(i => i == null);
                                var interfsNamesAndGenericSections = allNonNullInterfs.Select<Kind, string>
                                    (i =>
                                    {
                                        string stringToReturn = i.GetLongName(UseDefaultOnly, TargetPlatforms);

                                        string genericParamsSec = "";
                                        if (i.IsGenericInstance)
                                        {
                                            Kind[] genericArgs = i.GetGenericArguments();
                                            if (genericArgs.Length > 0)
                                            {
                                                genericParamsSec += GetGenericArgsOrParamsSection(kind, genericArgs);
                                            }
                                        }
                                        else if (i.IsGenericDefinition)
                                        {
                                            //generic params section------------------------------------------------------
                                            if (i.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                                            {
                                                genericParamsSec += i.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                            }
                                            else //this means Tril.Kind[] is returned
                                            {
                                                Kind[] genericParams = i.GetGenericParameters();
                                                if (genericParams.Length > 0)
                                                {
                                                    genericParamsSec += GetGenericArgsOrParamsSection(kind, genericParams);
                                                }
                                            }
                                        }

                                        stringToReturn += genericParamsSec;
                                        return stringToReturn;
                                    });

                                string joinedInterfs = string.Join(", ", interfsNamesAndGenericSections);
                                if (kind.GetTypeDefinition() != null && kind.GetTypeDefinition().IsSerializable) 
                                {
                                    joinedInterfs += ", java.io.Serializable";
                                }
                                TranslatedString.Append(joinedInterfs);
                                #endregion

                                #region OLD METHOD of concatenating interfaces
                                //foreach (Kind interf in interfaces)
                                //{
                                //    if (interf != null)
                                //    {
                                //        string genericParamsSec = "";
                                //        if (interf.IsGenericInstance)
                                //        {
                                //            Kind[] genericArgs = interf.GetGenericArguments();
                                //            if (genericArgs.Length > 0)
                                //            {
                                //                genericParamsSec += GetGenericArgsOrParamsSection(kind, genericArgs);
                                //            }
                                //        }
                                //        else if (interf.IsGenericDefinition)
                                //        {
                                //            //generic params section------------------------------------------------------
                                //            if (interf.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                                //            {
                                //                genericParamsSec += interf.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                //            }
                                //            else //this means Tril.Kind[] is returned
                                //            {
                                //                Kind[] genericParams = interf.GetGenericParameters();
                                //                if (genericParams.Length > 0)
                                //                {
                                //                    genericParamsSec += GetGenericArgsOrParamsSection(kind, genericParams);
                                //                }
                                //            }
                                //        }
                                //        TranslatedString.Append(interf.GetLongName(UseDefaultOnly, TargetPlatforms) + genericParamsSec + ", ");
                                //    }
                                //}
                                //if (kind.GetTypeDefinition() != null && kind.GetTypeDefinition().IsSerializable)
                                //    TranslatedString.Append("java.io.Serializable, ");
                                //string transString = TranslatedString.ToString().TrimEnd(' ').TrimEnd(',');
                                //TranslatedString = new StringBuilder(transString);
                                #endregion
                            }
                        }
                    }

                    //enter class block
                    TranslatedString.AppendLine().AppendLine(Indentation + "{");
                    EnterBlock();

                    //write fields------------------------------------------
                    foreach (Field field in kind.GetFields(UseDefaultOnly, TargetPlatforms))
                    {
                        TranslateField(field);
                    }
                    //Java allows the last field in an enum to end with "," so there is no need stressing myself to remove it
                    //if (TranslatedString.EndsWith(",\r\n")) //would be true if field is an enum field
                    //{
                    //    TranslatedString = TranslatedString.Substring(0, TranslatedString.Length - 3);//TranslatedString.TrimEnd('\n').TrimEnd('\r').TrimEnd(',');
                    //    TranslatedString += "\r\n";
                    //}

                    //write methods------------------------------------------
                    foreach (Method method in kind.GetMethods(UseDefaultOnly, TargetPlatforms))
                    {
                        TranslateMethod(method);
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
                        //create the file------------------------------------------
                        if (CurrentOutputDirectory != null)
                        {
                            string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".java");
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
                                string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".java");
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

                //important variables for implementing optional parameters as overloaded methods
                string methHeader = "", methName = "", genericParamSec = "", compParams = "", compParamNms = "";
                List<string> optParams = new List<string>(), optParamNms = new List<string>(), optParamVals = new List<string>();
                bool methRet = true;
                //set some of the values
                genericParamSec = method.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                dynamic retObj = method.GetReturnKind(UseDefaultOnly, TargetPlatforms);
                if (retObj == null)
                {
                    methRet = false;
                }
                else if (retObj is string)
                {
                    if (retObj.ToString() == "void")
                        methRet = false;
                }
                else if (retObj is Kind)
                {
                    Kind retKind = (Kind)retObj;
                    if (retKind.UnderlyingType.FullName == "System.Void")
                        methRet = false;
                }

                string currentMeaning = method.DeclaringKind.GetMeaning(UseDefaultOnly, TargetPlatforms);
                //change struct to class
                if (currentMeaning == "struct")
                    currentMeaning = "class";

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
                //write only user-defined annotations
                if (method.HasUserDefinedAnnotations(TargetPlatforms))
                {
                    string[] annotations = method.GetAnnotations(UseDefaultOnly, TargetPlatforms);
                    if (annotations.Length > 0)
                    {
                        foreach (string note in annotations)
                        {
                            if (note != null && note.Trim() != "")
                                TranslatedString.AppendLine(Indentation + "@" + note);
                        }
                    }
                }

                //method head-------------------------------------
                TranslatedString.Append(Indentation);

                //if (currentMeaning == "class") //apparently java can do these stuff for interfaces too
                {
                    //access modifier-------------------------------------
                    string[] accMods = method.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    if (accMods.Length > 0)
                    {
                        foreach (string accMod in accMods)
                        {
                            if (accMod != null && accMod.Trim() != "")
                            {
                                if (accMod.Trim() == "internal" || accMod.Trim() == "protected internal")
                                    methHeader += "protected ";
                                else
                                    methHeader += accMod.Trim() + " ";
                                break; //this break is because this lang expects only one access modifier on a method
                            }
                        }
                    }

                    //attributes-------------------------------------
                    string[] attris = method.GetAttributes(UseDefaultOnly, TargetPlatforms);
                    if (attris.Length > 0)
                    {
                        foreach (string attri in attris)
                        {
                            if (attri != null && attri.Trim() != "")
                            {
                                if (attri.Trim() == "sealed")
                                    methHeader += "final ";
                                else if (attri.Trim() != "virtual" && attri.Trim() != "override") //java doesnt have "virtual" and "override"
                                    methHeader += attri.Trim() + " ";
                            }
                        }
                    }
                }

                //generic-------------------------------------
                //if (method.IsGenericInstance) //comment out this block as we are strictly interested in the methods gen params ONLY, not args
                //{
                //    Kind[] genericArgs = method.GetGenericArguments();
                //    if (genericArgs.Length > 0)
                //    {
                //        methHeader += GetGenericArgsOrParamsSection(method.DeclaringKind, genericArgs) + " ";
                //    }
                //}
                //else 
                if (method.IsGenericDefinition)
                {
                    //generic params section------------------------------------------------------
                    if (method.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                    {
                        methHeader += method.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms) + " ";
                    }
                    else //this means Tril.Kind[] is returned
                    {
                        Kind[] genericParams = method.GetGenericParameters();
                        if (genericParams.Length > 0)
                        {
                            methHeader += GetGenericArgsOrParamsSection(method.DeclaringKind, genericParams) + " ";
                        }
                    }
                }

                //return type-------------------------------------
                if (!method.IsConstructor)
                {
                    string retTypeStr = "";

                    if (method.HasUserDefinedReturnKind(TargetPlatforms)) //this means the return type would be returned as a string
                    {
                        retTypeStr = (string)method.GetReturnKind(UseDefaultOnly, TargetPlatforms);
                    }
                    else //this means the return type would be returned as a Tril.Kind
                    {
                        Kind returnKind = (Kind)method.GetReturnKind(UseDefaultOnly, TargetPlatforms);
                        if (returnKind != null)
                        {
                            retTypeStr = GetAppropriateName(method.DeclaringKind, returnKind);
                            if (returnKind.IsGenericInstance)
                            {
                                Kind[] genericArgs = returnKind.GetGenericArguments();
                                if (genericArgs.Length > 0)
                                {
                                    retTypeStr += GetGenericArgsOrParamsSection(method.DeclaringKind, genericArgs);
                                }
                            }
                            else if (returnKind.IsGenericDefinition)
                            {
                                //generic params section------------------------------------------------------
                                if (returnKind.HasUserDefinedGenericParametersSection(TargetPlatforms)) //this means string is returned
                                {
                                    retTypeStr += returnKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                }
                                else //this means Tril.Kind[] is returned
                                {
                                    Kind[] genericParams = returnKind.GetGenericParameters();
                                    if (genericParams.Length > 0)
                                    {
                                        retTypeStr += GetGenericArgsOrParamsSection(method.DeclaringKind, genericParams);
                                    }
                                }
                            }
                        }
                    }
                    if (retTypeStr != null && retTypeStr.Trim() != "")
                    {
                        methHeader += retTypeStr.Trim() + " ";
                    }
                }

                //name-------------------------------------
                methName += method.GetName(UseDefaultOnly, TargetPlatforms);
                methHeader += methName;
                TranslatedString.Append(methHeader);

                //parameters-------------------------------------
                if (method.HasUserDefinedParameterSection(TargetPlatforms)) //this means string is returned
                {
                    TranslatedString.Append((string)method.GetParameters(UseDefaultOnly, TargetPlatforms));
                }
                else //this means Tril.Parameter[] is returned
                {
                    string paramSection = "";
                    Parameter[] paramz = (Parameter[])method.GetParameters(UseDefaultOnly, TargetPlatforms);
                    foreach (Parameter param in paramz)
                    {
                        dynamic paramKind = param.GetParameterKind(UseDefaultOnly, TargetPlatforms);
                        string paramKindStr = "";

                        if (paramKind != null)
                        {
                            if (paramKind is Kind)
                            {
                                Kind paramKindObj = paramKind as Kind;
                                if (param.IsVarArg && paramKindObj.UnderlyingType.IsArray)
                                {
                                    Kind paramKindElementObj = Kind.GetCachedKind(paramKindObj.UnderlyingType.GetElementType());
                                    paramKindStr = GetAppropriateName(method.DeclaringKind, paramKindElementObj) + "...";
                                }
                                else
                                    paramKindStr = GetAppropriateName(method.DeclaringKind, paramKindObj);
                            }
                            else //if (paramKind is string)
                            {
                                paramKindStr = paramKind.ToString();
                            }

                            paramKindStr = paramKindStr.Replace("&", "").Replace("*", ""); //remove '*' and '&' for pointers
                            paramKindStr += " ";
                        }

                        string paramLongName = paramKindStr + param.GetName(UseDefaultOnly, TargetPlatforms);

                        if (param.IsOptional && param.HasConstantValue)
                        {
                            optParams.Add(paramLongName.Trim());
                            optParamNms.Add(param.GetName(UseDefaultOnly, TargetPlatforms));
                            optParamVals.Add(param.ConstantValue.ToString());
                        }
                        else
                        {
                            compParams += paramLongName.Trim() + ", ";
                            compParamNms += param.GetName(UseDefaultOnly, TargetPlatforms) + ", ";
                        }

                        if (paramLongName != null && paramLongName.Trim() != "")
                        {
                            paramSection += paramLongName.Trim() + ", ";
                        }
                    }
                    paramSection = paramSection.TrimEnd(' ').TrimEnd(',');
                    //paramSection = paramSection.TrimEnd(',');
                    TranslatedString.Append("(" + paramSection + ")");

                    compParams = compParams.TrimEnd(' ').TrimEnd(',');
                    compParamNms = compParamNms.TrimEnd(' ').TrimEnd(',');
                }

                //pre-method body---------------------------------
                string preBody = method.GetPreBodySection(UseDefaultOnly, TargetPlatforms);
                if (preBody != null && preBody.Trim() != "")
                    TranslatedString.Append(" " + preBody.Trim());

                //method body-------------------------------------
                Exception exception = null;

                if (currentMeaning == "class")
                {
                    try
                    {
                        string methodBodyStr = "";
                        Block methodBody = method.GetBody(CodomTranslator, ReturnPartial, Optimize, UseDefaultOnly, TargetPlatforms);
                        if (methodBody != null)
                        {
                            methodBodyStr += " {\r\n";
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
                }
                else //if (currentMeaning.EndsWith("interface")) //interface or @interface
                {
                    TranslatedString.AppendLine(";");
                }

                //create overloaded methods to handle optional params
                int max = optParamVals.Count() - 1;
                for (int index = 0; index <= max; index++)
                {
                    TranslatedString.Append(Indentation + methHeader + "(" + compParams);
                    if (index > 0)
                    {
                        string optParamsSec = "";
                        for (int idx = 0; idx < index; idx++)
                        {
                            optParamsSec += optParams[idx] + ", ";
                        }
                        optParamsSec = optParamsSec.TrimEnd(' ').TrimEnd(',');
                        if (optParamsSec.Trim() != "")
                            TranslatedString.Append(", " + optParamsSec.Trim());
                    }
                    TranslatedString.Append(")");
                    if (preBody != null && preBody.Trim() != "")
                        TranslatedString.Append(" " + preBody.Trim());
                    TranslatedString.AppendLine(" {");
                    EnterBlock();
                    TranslatedString.Append(Indentation + (methRet ? "return " : "") + methName + genericParamSec + "(" + compParamNms);
                    if (index > 0)
                    {
                        string optParamNmsSec = "";
                        for (int idx = 0; idx < index; idx++)
                        {
                            optParamNmsSec += optParamNms[idx] + ", ";
                        }
                        optParamNmsSec = optParamNmsSec.TrimEnd(' ').TrimEnd(',');
                        if (optParamNmsSec.Trim() != "")
                            TranslatedString.Append(", " + optParamNmsSec.Trim());
                    }
                    string optParamValsSec = "";
                    for (int idx = index; idx <= max; idx++)
                    {
                        optParamValsSec += optParamVals[idx] + ", ";
                    }
                    optParamValsSec = optParamValsSec.TrimEnd(' ').TrimEnd(',');
                    if (optParamValsSec.Trim() != "")
                        TranslatedString.Append(", " + optParamValsSec.Trim());
                    TranslatedString.AppendLine(");");
                    ExitBlock();
                    TranslatedString.AppendLine(Indentation + "}");
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
                        if (TargetTypes.Any(t => t == kind.GetLongName(UseDefaultOnly, TargetPlatforms) || t == kind.GetLongName(true)))
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
            throw new NotSupportedException("Java does not support properties!");
        }
    }
}
