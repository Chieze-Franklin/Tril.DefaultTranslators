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
    public class ToCSharpTranslator : DefaultTranslator
    {
        public ToCSharpTranslator()
        {
            TargetPlatforms = new string[] { "cs", "*" };
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
                {
                    if (catchCode.CachedCaughtExceptionKind == "System.Object")
                        codeString += prefix + "catch {\r\n";
                    //else if (new Exception().GetType().IsAssignableFrom(catchCode.CaughtExceptionKind.UnderlyingType))
                    //    codeString += prefix + "catch {\r\n";
                    else
                        codeString += prefix + "catch(" + catchCode.CachedCaughtExceptionKind + ") {\r\n";
                }
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
            else if (code is Filter)
            {
                Filter filterCode = (code as Filter);
                codeString += prefix + "/*filter*/ {\r\n";
                EnterBlock();
                foreach (Codom filteryBodyCode in filterCode)
                {
                    codeString += GetCodeString(filteryBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
            }
            else if (code is FilterHandler)
            {
                FilterHandler filterHandlerCode = (code as FilterHandler);
                codeString += prefix + "/*filter-handler*/ {\r\n";
                EnterBlock();
                foreach (Codom filteryHandlerBodyCode in filterHandlerCode)
                {
                    codeString += GetCodeString(filteryHandlerBodyCode);
                }
                ExitBlock();
                codeString += prefix + "}\r\n";
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

            //handle AddressOf, PointerRef
            //
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
            if (code is Compare)
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
            else if (code is DynamicArglist)
            {
                DynamicArglist arglistCode = (code as DynamicArglist);
                if (arglistCode.IsInline)
                    return "__arglist";
                return "__arglist;";
            }
            else if (code is DynamicMakeRef)
            {
                DynamicMakeRef makerefCode = (code as DynamicMakeRef);
                if (makerefCode.IsInline)
                    return "__makeref(" + GetCodeString(makerefCode.Pointer) + ")";
                return "__makeref(" + GetCodeString(makerefCode.Pointer) + ");";
            }
            else if (code is DynamicRefType)
            {
                DynamicRefType reftypeCode = (code as DynamicRefType);
                if (reftypeCode.IsInline)
                    return "__reftype(" + GetCodeString(reftypeCode.TypedReference) + ")";
                return "__reftype(" + GetCodeString(reftypeCode.TypedReference) + ");";
            }
            else if (code is DynamicRefValue)
            {
                DynamicRefValue refvalueCode = (code as DynamicRefValue);
                if (refvalueCode.IsInline)
                    return "__refvalue(" + GetCodeString(refvalueCode.TypedReference) + ", " + refvalueCode.TargetKind.GetLongName(UseDefaultOnly, TargetPlatforms) + ")";
                return "__refvalue(" + GetCodeString(refvalueCode.TypedReference) + ", " + refvalueCode.TargetKind.GetLongName(UseDefaultOnly, TargetPlatforms) + ");";
            }
            //else if (code is IndirectAssignment)
            //{
            //    IntDiv intDivCode = (code as IntDiv);
            //    if (intDivCode.IsInline)
            //        return (intDivCode.ShowOuterBrackets ? "(" : "") + GetCodeString(intDivCode.FirstOperand) + " = " + GetCodeString(intDivCode.SecondOperand) + (intDivCode.ShowOuterBrackets ? ")" : "");
            //    else
            //        return GetCodeString(intDivCode.FirstOperand) + " = " + GetCodeString(intDivCode.SecondOperand) + ";";
            //}
            else if (code is IntDiv)
            {
                IntDiv intDivCode = (code as IntDiv);
                if (intDivCode.IsInline)
                    return (intDivCode.ShowOuterBrackets ? "(" : "") + "(int)(" + GetCodeString(intDivCode.FirstOperand) + " / " + GetCodeString(intDivCode.SecondOperand) + ")" + (intDivCode.ShowOuterBrackets ? ")" : "");
                else
                    return "(int)(" + GetCodeString(intDivCode.FirstOperand) + " / " + GetCodeString(intDivCode.SecondOperand) + ");";
            }
            //else if (code is LocAlloc)
            else if (code is MethodRef)
            {
                try
                {
                    MethodRef methCode = (code as MethodRef);
                    //I will be looking for property and event methods
                    if (methCode is StaticMethodRef) //I will be looking for overloaded operators here
                    {
                        StaticMethodRef statMethCode = (methCode as StaticMethodRef);

                        #region Why I am not changing "op_Decrement" and "op_Increment" to "--" and "++"
                        //I'm no longer going to format "op_Decrement" and "op_Increment" because:
                        //1. I can't decide if the operators should come before or after the operands
                        //2. Changing them to "--" and "++" produces a logically different code from the original source code
                        //      because of the way the C# compiler treats the "op_Decrement" and "op_Increment" methods. For instance,
                        //      the "op_Decrement" and "op_Increment" methods do not change the values of their arguments/operands (because) the
                        //      arguments are not passed by ref, but the "--" and "++" operands change the values of their operands as though
                        //      they are passed by ref. Hence "++a;" is a complete statement on its own; to represent this the compiler does NOT
                        //      generate "op_Increment(a);" but "a = op_Increment(a);." The problem becomes obvious when you have
                        //      b = ++a;
                        //      for which the compiler does not generate "b = op_Increment(a)" as you would expect but:
                        //      a = op_Increment(a); b = op_Increment(a);
                        //      The "op_Increment" is called twice NOT once as you might expect. I expected:
                        //      a = op_Increment(a); b = a;
                        //if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Decrement")
                        //{
                        //    ValueStatement operand = statMethCode.Arguments[0];
                        //    string returnStr = GetCodeString(operand) + "--";
                        //    if (statMethCode.IsInline)
                        //        return returnStr;
                        //    else
                        //        return returnStr + ";";
                        //}
                        //else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Increment")
                        //{
                        //    ValueStatement operand = statMethCode.Arguments[0];
                        //    string returnStr = GetCodeString(operand) + "++";
                        //    if (statMethCode.IsInline)
                        //        return returnStr;
                        //    else
                        //        return returnStr + ";";
                        //}
                        //else 
                        #endregion
                        if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_UnaryNegation" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_UnaryPlus" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_OnesComplement" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_LogicalNot")
                        {
                            ValueStatement operand = statMethCode.Arguments[0];

                            string op = "";
                            if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_UnaryNegation")
                                op = "-";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_UnaryPlus")
                                op = "+";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_OnesComplement")
                                op = "~";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_LogicalNot")
                                op = "!";

                            string returnStr = op + GetCodeString(operand);
                            if (statMethCode.IsInline)
                                return returnStr;
                            else
                                return returnStr + ";";
                        }
                        //I can't format "op_Implicit" and "op_Explicit" because their arguments don't tell us what type the conversion is to.
                        //These methods are overloaded not by arguments but by return types (they are about the only methods in C# allowed such power)
                        //I'm also not touching "op_True", "op_False"
                        else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Addition" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Subtraction" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Multiply" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Division" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Modulus" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_ExclusiveOr" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_BitwiseAnd" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_BitwiseOr" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_LeftShift" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_RightShift" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Equality" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Inequality" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_GreaterThan" ||
                            statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_LessThan")
                        {
                            ValueStatement left = statMethCode.Arguments[0];
                            ValueStatement right = statMethCode.Arguments[1];

                            string op = "";
                            if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Addition")
                                op = " + ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Subtraction")
                                op = " - ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Multiply")
                                op = " * ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Division")
                                op = " / ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Modulus")
                                op = " % ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_ExclusiveOr")
                                op = " ^ ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_BitwiseAnd")
                                op = " & ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_BitwiseOr")
                                op = " | ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_LeftShift")
                                op = " << ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_RightShift")
                                op = " >> ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Equality")
                                op = " == ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_Inequality")
                                op = " != ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_GreaterThan")
                                op = " > ";
                            else if (statMethCode.ReferencedMethod.UnderlyingMethod.Name == "op_LessThan")
                                op = " < ";

                            string returnStr = GetCodeString(left) + op + GetCodeString(right);
                            if (statMethCode.IsInline)
                                return returnStr;
                            else
                                return returnStr + ";";
                        }
                    }

                    return null;
                }
                catch 
                {
                    return null;
                }
            }
            else if (code is ShiftRight)
            {
                ShiftRight shrCode = (code as ShiftRight);
                if (shrCode.IsInline)
                    return (shrCode.ShowOuterBrackets ? "(" : "") + (shrCode.IsUnsigned ? "(int)((uint)" : "") + GetCodeString(shrCode.FirstOperand) + " >> " + GetCodeString(shrCode.SecondOperand) + (shrCode.IsUnsigned ? ")" : "") + (shrCode.ShowOuterBrackets ? ")" : "");
                else
                    return (shrCode.IsUnsigned ? "(int)((uint)" : "") + GetCodeString(shrCode.FirstOperand) + " >> " + GetCodeString(shrCode.SecondOperand) + (shrCode.IsUnsigned ? ")" : "") + ";";
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

                string currentMeaning = _event.DeclaringKind.GetMeaning(UseDefaultOnly, TargetPlatforms);

                //write comments--------------------------------------
                string[] comments = _event.GetComments(UseDefaultOnly, TargetPlatforms);
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
                //show only default attributes
                string[] annotations = _event.GetAnnotations(UseDefaultOnly/*true*/, TargetPlatforms);
                if (annotations.Length > 0)
                {
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "[" + note + "]");
                    }
                }

                //if (currentMeaning == "enum")// && decKindStr != null && decKindStr.Trim() != "" && decKindStr.Trim() == fldTypeStr.Trim())
                //{
                //    //if fldTypeStr == the name of the declaring kind, dont show it
                //    string fldTypeStr = "";

                //    if (property.HasUserDefinedPropertyKind(TargetPlatforms)) //this means the field type would be returned as a string
                //    {
                //        fldTypeStr = property.GetPropertyKind(UseDefaultOnly, TargetPlatforms);
                //    }
                //    else //this means the field type would be returned as a Tril.Kind
                //    {
                //        Kind fieldKind = property.GetPropertyKind(UseDefaultOnly, TargetPlatforms);
                //        if (fieldKind != null)
                //        {
                //            fldTypeStr = fieldKind.GetLongName(UseDefaultOnly, TargetPlatforms);
                //        }
                //    }
                //    if (property.DeclaringKind.GetLongName(UseDefaultOnly, TargetPlatforms) == fldTypeStr.Trim())
                //    {
                //        //name-------------------------------------
                //        TranslatedString += Indentation + property.GetName(UseDefaultOnly, TargetPlatforms) + ",\r\n";
                //    }
                //}
                //else
                //{
                TranslatedString.Append(Indentation);

                //access modifier---------------------------------
                string[] accMods = _event.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                if (accMods.Length > 0)
                {
                    foreach (string accMod in accMods)
                    {
                        if (accMod != null && accMod.Trim() != "")
                        {
                            TranslatedString.Append(accMod.Trim() + " ");
                            break; //this break is because this lang expects only one access modifier on a method
                        }
                    }
                }

                //attributes-------------------------------------
                string[] attris = _event.GetAttributes(UseDefaultOnly, TargetPlatforms);
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

                //event type-------------------------------------
                string evtTypeStr = "";

                if (_event.HasUserDefinedEventHandlerKind(TargetPlatforms)) //this means the field type would be returned as a string
                {
                    evtTypeStr = _event.GetEventHandlerKind(UseDefaultOnly, TargetPlatforms);
                }
                else //this means the event type would be returned as a Tril.Kind
                {
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
                }
                if (evtTypeStr != null && evtTypeStr.Trim() != "")
                {
                    TranslatedString.Append("event " + evtTypeStr.Trim() + " ");
                }

                //name-------------------------------------
                TranslatedString.AppendLine(_event.GetName(UseDefaultOnly, TargetPlatforms));
                TranslatedString.AppendLine(Indentation + "{");
                EnterBlock();
                Exception exception = null;
                if (_event.HasAddMethod)
                {
                    TranslatedString.Append(Indentation + "add");
                    try
                    {
                        TranslateMethodBody(_event.AddMethod);
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
                if (_event.HasRemoveMethod)
                {
                    TranslatedString.Append(Indentation + "remove");
                    try
                    {
                        TranslateMethodBody(_event.RemoveMethod);
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
                ExitBlock();
                TranslatedString.AppendLine(Indentation + "}");
                //}

                OnEventTranslated(_event, TranslatedString.ToString().Substring(start), (exception == null), exception);
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

                string currentMeaning = field.DeclaringKind.GetMeaning(UseDefaultOnly, TargetPlatforms);

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
                //show only default attributes
                string[] annotations = field.GetAnnotations(UseDefaultOnly/*true*/, TargetPlatforms);
                if (annotations.Length > 0)
                {
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "[" + note + "]");
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

                    //access modifier---------------------------------
                    string[] accMods = field.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    if (accMods.Length > 0)
                    {
                        foreach (string accMod in accMods)
                        {
                            if (accMod != null && accMod.Trim() != "")
                            {
                                TranslatedString.Append(accMod.Trim() + " ");
                                break; //this break is because this lang expects only one access modifier on a method
                            }
                        }
                    }

                    //attributes-------------------------------------
                    string[] attris = field.GetAttributes(UseDefaultOnly, TargetPlatforms);
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
                                fldTypeStr += fieldKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                            }
                            else if (fieldKind.IsGenericDefinition)
                            {
                                fldTypeStr += fieldKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
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

                        //write imported namespaces-----------------------------------------
                        string[] imports = kind.GetImports(UseDefaultOnly, TargetPlatforms);
                        if (imports.Length > 0)
                        {
                            foreach (string import in imports)
                            {
                                if (import != null && import.Trim() != "")
                                    TranslatedString.AppendLine(Indentation + "using " + import.Trim() + ";");
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
                    //use default attributes only
                    string[] annotations = kind.GetAnnotations(UseDefaultOnly/*true*/, TargetPlatforms);
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
                    string[] accMods = kind.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    if (accMods.Length > 0)
                    {
                        foreach (string accMod in accMods)
                        {
                            if (accMod != null && accMod.Trim() != "")
                            {
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
                                    TranslatedString.Append(attri.Trim() + " ");
                                }
                            }
                        }
                    }

                    //name------------------------------------------------------
                    TranslatedString.Append(kindMeaning + " " + kind.GetName(UseDefaultOnly, TargetPlatforms));
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
                    if (kindMeaning == "class")// || kindMeaning == "struct" || kindMeaning == "enum")
                    {
                        baseKinds = kind.GetBaseKinds(UseDefaultOnly, TargetPlatforms);
                        if (baseKinds != null) //baseKinds can be null for interfaces
                        {
                            if (kind.HasUserDefinedBaseKinds(TargetPlatforms)) //this means kind.GetBaseKinds returns a string[]
                            {
                                string[] baseKindsArray = (string[])baseKinds;
                                if (baseKindsArray.Length == 1 && baseKindsArray[0] != null)
                                    if ((baseKindsArray[0]).Trim() != "")
                                        TranslatedString.Append(" : " + (baseKindsArray[0]).Trim());
                            }
                            else //this means kind.BaseKind returns a single Tril.Kind
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
                                if (baseKinds != null && interfaces.Any(i => (i != null && i.Trim() != "")))
                                    TranslatedString.AppendLine(",").Append(Indentation + "\t");

                                #region NEW METHOD of concatenating interfaces
                                //bool noElemIsNullOrEmpty = interfaces.All(i => i != null && i.Trim() != "");
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
                        else if (kindMeaning != "enum" && kindMeaning != "struct")//this means kind.GetInterfaces() returns a Kind[]
                        {
                            Kind[] interfaces = (Kind[])kindInterfaces;
                            if (interfaces.Length > 0)
                            {
                                if (baseKinds != null && interfaces.Any(i => i != null))
                                    TranslatedString.AppendLine(",").Append(Indentation + "\t");

                                #region NEW METHOD of concatenating interfaces
                                var allNonNullInterfs = interfaces.SkipWhile(i => i == null);
                                var interfsNamesAndGenericSections = allNonNullInterfs.Select<Kind, string>
                                    (i => 
                                    {
                                        string stringToReturn = i.GetLongName(UseDefaultOnly, TargetPlatforms);
                                        if (i.IsGenericInstance)
                                        {
                                            stringToReturn += i.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                        }
                                        else if (i.IsGenericDefinition)
                                        {
                                            stringToReturn += i.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                                        }
                                        return stringToReturn;
                                    });
                                string joinedInterfs = string.Join(", ", interfsNamesAndGenericSections);
                                TranslatedString.Append(joinedInterfs);
                                #endregion

                                #region OLD METHOD of concatenating interfaces
                                //foreach (Kind interf in interfaces)
                                //{
                                //    if (interf != null)
                                //    {
                                //        TranslatedString.Append(interf.GetLongName(UseDefaultOnly, TargetPlatforms));
                                //        if (interf.IsGenericInstance)
                                //        {
                                //            TranslatedString.Append(interf.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                                //        }
                                //        else if (interf.IsGenericDefinition)
                                //        {
                                //            TranslatedString.Append(interf.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms));
                                //        }
                                //        TranslatedString.Append(", ");
                                //    }
                                //}
                                //string transString = TranslatedString.ToString().TrimEnd(' ').TrimEnd(',');
                                //TranslatedString = new StringBuilder(transString);
                                #endregion
                            }
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
                    //C# allows the last field in an enum to end with "," so there is no need stressing myself to remove it
                    //if (TranslatedString.EndsWith(",\r\n")) //would be true if field is an enum field
                    //{
                    //    TranslatedString = TranslatedString.Substring(0, TranslatedString.Length - 3);//TranslatedString.TrimEnd('\n').TrimEnd('\r').TrimEnd(',');
                    //    TranslatedString += "\r\n";
                    //}

                    //write methods------------------------------------------
                    foreach (Method method in kind.GetMethods(UseDefaultOnly, TargetPlatforms))
                    {
                        if (!method.IsPropertyMethod && !method.IsEventMethod) //dont write them now, write them in there properties and events
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
                        if (_namespace != null)
                        {
                            if (_namespace.Trim() != "")
                            {
                                ExitBlock();
                                TranslatedString.Append(Indentation + "}");
                            }
                        }

                        //create the file------------------------------------------
                        if (CurrentOutputDirectory != null)
                        {
                            string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".cs");
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
                                string filePath = GetValidFilePath(CurrentOutputDirectory + "\\" + kind.GetName(UseDefaultOnly, TargetPlatforms) + ".cs");
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

        public override void TranslateMethod(Method method) //**************cant differentiate btw "safe" and "unsafe"
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

                string currentMeaning = method.DeclaringKind.GetMeaning(UseDefaultOnly, TargetPlatforms);

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
                //use default attributes only
                string[] annotations = method.GetAnnotations(UseDefaultOnly/*true*/, TargetPlatforms);
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
                if (currentMeaning == "class" || currentMeaning == "struct")
                {
                    string[] accMods = method.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                    if (accMods.Length > 0)
                    {
                        foreach (string accMod in accMods)
                        {
                            if (accMod != null && accMod.Trim() != "")
                            {
                                TranslatedString.Append(accMod.Trim() + " ");
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
                                TranslatedString.Append(attri.Trim() + " ");
                            }
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
                                retTypeStr += returnKind.GetGenericArgumentsSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                            }
                            else if (returnKind.IsGenericDefinition)
                            {
                                retTypeStr += returnKind.GetGenericParametersSection_CsStyle(UseDefaultOnly, TargetPlatforms);
                            }
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
                                paramKindStr = GetAppropriateName(method.DeclaringKind, paramKindObj);
                            }
                            else //if (paramKind is string)
                            {
                                paramKindStr = paramKind.ToString();
                            }

                            //**************be careful with the below line
                            //**************the reason being that C# can be both "safe" and "unsafe"
                            //**************we have to device a way of knowing which mode it is in
                            //**************that would determine if the below line is commented or not
                            paramKindStr = paramKindStr.Replace("&", "").Replace("*", ""); //remove '*' and '&' for pointers
                            paramKindStr += " ";
                        }

                        string paramLongName = paramKindStr + param.GetName(UseDefaultOnly, TargetPlatforms);

                        //optional param
                        if (param.IsOptional && param.HasConstantValue) 
                        {
                            paramLongName += " = " + param.ConstantValue;
                        }

                        if (paramLongName != null && paramLongName.Trim() != "")
                        {
                            string paramAttriStr = "";
                            foreach (string paramAttri in param.GetAttributes(UseDefaultOnly, TargetPlatforms))
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
                }

                //generic constraints
                TranslatedString.Append(" " + method.GetGenericConstraintsSection_CsStyle(UseDefaultOnly, TargetPlatforms));

                //pre-method body---------------------------------
                string preBody = method.GetPreBodySection(UseDefaultOnly, TargetPlatforms);
                if (preBody != null && preBody.Trim() != "")
                    TranslatedString.Append(" " + preBody.Trim());

                //method body-------------------------------------
                Exception exception = null;

                if (currentMeaning == "class" || currentMeaning == "struct")
                {
                    try
                    {
                        TranslateMethodBody(method);
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

                OnMethodTranslated(method, TranslatedString.ToString().Substring(start), (exception == null), exception);
            }
            catch (Exception e)
            {
                OnMethodTranslated(method, TranslatedString.ToString().Substring(start), false, e);
            }
        }
        void TranslateMethodBody(Method method)
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

                string currentMeaning = property.DeclaringKind.GetMeaning(UseDefaultOnly, TargetPlatforms);

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
                //show only default attributes
                string[] annotations = property.GetAnnotations(UseDefaultOnly/*true*/, TargetPlatforms);
                if (annotations.Length > 0)
                {
                    foreach (string note in annotations)
                    {
                        if (note != null && note.Trim() != "")
                            TranslatedString.AppendLine(Indentation + "[" + note + "]");
                    }
                }

                //if (currentMeaning == "enum")// && decKindStr != null && decKindStr.Trim() != "" && decKindStr.Trim() == fldTypeStr.Trim())
                //{
                //    //if fldTypeStr == the name of the declaring kind, dont show it
                //    string fldTypeStr = "";

                //    if (property.HasUserDefinedPropertyKind(TargetPlatforms)) //this means the field type would be returned as a string
                //    {
                //        fldTypeStr = property.GetPropertyKind(UseDefaultOnly, TargetPlatforms);
                //    }
                //    else //this means the field type would be returned as a Tril.Kind
                //    {
                //        Kind fieldKind = property.GetPropertyKind(UseDefaultOnly, TargetPlatforms);
                //        if (fieldKind != null)
                //        {
                //            fldTypeStr = fieldKind.GetLongName(UseDefaultOnly, TargetPlatforms);
                //        }
                //    }
                //    if (property.DeclaringKind.GetLongName(UseDefaultOnly, TargetPlatforms) == fldTypeStr.Trim())
                //    {
                //        //name-------------------------------------
                //        TranslatedString += Indentation + property.GetName(UseDefaultOnly, TargetPlatforms) + ",\r\n";
                //    }
                //}
                //else
                //{
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
                                break; //this break is because this lang expects only one access modifier on a method
                            }
                        }
                    }

                    //attributes-------------------------------------
                    string[] attris = property.GetAttributes(UseDefaultOnly, TargetPlatforms);
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

                    if (property.HasUserDefinedPropertyKind(TargetPlatforms)) //this means the field type would be returned as a string
                    {
                        propTypeStr = property.GetPropertyKind(UseDefaultOnly, TargetPlatforms);
                    }
                    else //this means the field type would be returned as a Tril.Kind
                    {
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
                    }
                    if (propTypeStr != null && propTypeStr.Trim() != "")
                    {
                        TranslatedString.Append(propTypeStr.Trim() + " ");
                    }

                    //name-------------------------------------
                    TranslatedString.AppendLine(property.GetName(UseDefaultOnly, TargetPlatforms));
                    TranslatedString.AppendLine(Indentation + "{");
                    EnterBlock();
                    Exception exception = null;
                    if (property.HasGetMethod)
                    {
                        TranslatedString.Append(Indentation);
                        //get method access modifier---------------------------------
                        string[] getAccMods = property.GetMethod.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                        if (getAccMods.Length > 0)
                        {
                            foreach (string accMod in getAccMods)
                            {
                                //the last condition below "!accMods.Contains(accMod)" is to ensure
                                //that if the access modifier is already specified in the containing property
                                //we do not repeat in here
                                //i.e. the access modifier is only specified if it is different from that of the containing property
                                if (accMod != null && accMod.Trim() != "" && !accMods.Contains(accMod))
                                {
                                    TranslatedString.Append(accMod.Trim() + " ");
                                    break; //this break is because this lang expects only one access modifier on a method
                                }
                            }
                        }
                        TranslatedString.Append("get");

                        try
                        {
                            TranslateMethodBody(property.GetMethod);
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
                    if (property.HasSetMethod)
                    {
                        TranslatedString.Append(Indentation);
                        //get method access modifier---------------------------------
                        string[] setAccMods = property.SetMethod.GetAccessModifiers(UseDefaultOnly, TargetPlatforms);
                        if (setAccMods.Length > 0)
                        {
                            foreach (string accMod in setAccMods)
                            {
                                //the last condition below "!accMods.Contains(accMod)" is to ensure
                                //that if the access modifier is already specified in the containing property
                                //we do not repeat in here
                                //i.e. the access modifier is only specified if it is different from that of the containing property
                                if (accMod != null && accMod.Trim() != "" && !accMods.Contains(accMod))
                                {
                                    TranslatedString.Append(accMod.Trim() + " ");
                                    break; //this break is because this lang expects only one access modifier on a method
                                }
                            }
                        }
                        TranslatedString.Append("set");

                        try
                        {
                            TranslateMethodBody(property.SetMethod);
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
                    ExitBlock();
                    TranslatedString.AppendLine(Indentation + "}");
                //}

                OnPropertyTranslated(property, TranslatedString.ToString().Substring(start), (exception == null), exception);
            }
            catch (Exception e)
            {
                OnPropertyTranslated(property, TranslatedString.ToString().Substring(start), false, e);
            }
        }
    }
}
