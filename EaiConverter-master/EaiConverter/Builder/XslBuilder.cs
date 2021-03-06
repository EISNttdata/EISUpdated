﻿using EaiConverter.Utils;

namespace EaiConverter.Builder
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using EaiConverter.CodeGenerator.Utils;
    using EaiConverter.Parser.Utils;

    public class XslBuilder
    {
        private IXpathBuilder xpathBuilder;
        
        private Tab tab = new Tab();

        private Dictionary<string, string> variableDecleration;

        public XslBuilder(IXpathBuilder xpathBuilder)
        {
            this.xpathBuilder = xpathBuilder;
        }

        public CodeStatementCollection Build(IEnumerable<XNode> inputNodes)
        {
            return this.Build(string.Empty, inputNodes);
        }

        public CodeStatementCollection Build(string packageName, IEnumerable<XNode> inputNodes)
        {
            this.tab = new Tab();
            this.variableDecleration = new Dictionary<string, string>();
            var newPackageName = FormatCorrectlyPackageName(packageName);
            var codeInStringList = this.Build(newPackageName, inputNodes, null);
            
            string codeinString = this.GenerateVariableDeclaration() + codeInStringList;
            // TODO : remove this ugly fix !!!
            codeinString = MyUglyModification(codeinString);

            var codeSnippet = new CodeSnippetStatement(codeinString);
            var codeStatements = new CodeStatementCollection();
            codeStatements.Add(codeSnippet);
            return codeStatements;
        }

        public void AddVariableDecleration(string variableName, string variableCode)
        {
            if (!this.variableDecleration.ContainsKey(variableName))
            {
                this.variableDecleration.Add(variableName, variableCode);
            }
        }

        public string GenerateVariableDeclaration()
        {
            var sb = new StringBuilder();
            foreach (var value in this.variableDecleration.Values)
            {
                sb.AppendLine(value);
            }
            return sb.ToString();
        }

        private static string MyUglyModification(string codeinString)
        {
            codeinString = codeinString.Replace("NTMMessage.NTMTrade.", "((NTMTrade)NTMMessage.Items[0]).");
            codeinString = codeinString.Replace("NTMMessage.NTMTrades", "NTMMessage.Items");
            codeinString = codeinString.Replace("NTMMessage.NTMTrade", "NTMMessage.Items[0]");
            codeinString = codeinString.Replace(".Resultsets.ResultSet1[1]1[1]", string.Empty);
            return codeinString;
        }

        private StringBuilder Build(IEnumerable<XNode> inputNodes, string parent)
        {
            return Build(string.Empty, inputNodes, parent);
        }

        private StringBuilder Build(string packageName, IEnumerable<XNode> inputNodes, string parent)
        {
            var codeStatements = new StringBuilder();
            if (inputNodes == null)
            {
                return codeStatements;
            }

            bool isAlistElement = false;

            var listElements = new Dictionary<string, int>();

            foreach (var inputNode in inputNodes)
            {
                //check instance
                //    var element="";
                //  var elementType = typeof(inputNode);
                //  if (inputNode.GetType().IsInstanceOfType(typeof(XElement)))
                //  {
                //     element = (XElement)inputNode;
                // }
                // else if (inputNode.GetType().IsInstanceOfType(typeof(XComment)))
                // {
                //    element = (XComment)inputNode;
                // }

                //var element = (XElement)inputNode;
                var element = inputNode.GetType().IsInstanceOfType(typeof(XElement)) ? (XElement)inputNode: null;

               
                if (element!=null && !Regex.IsMatch(element.Name.NamespaceName, XmlnsConstant.xslNameSpace))
                {
                    string returnType = DefineReturnType(parent, element);
                    if (IsBasicReturnType(returnType))
                    {
                        packageName = string.Empty;
                    }

                    string variableReference = this.DefineVariableReference(element, parent);
                    isAlistElement = this.IsAListElement(element, inputNodes);
                    var hasTheListBeenInitialised = false;

                    if (isAlistElement)
                    {
                        hasTheListBeenInitialised = listElements.ContainsKey(element.Name.LocalName);

                        if (!hasTheListBeenInitialised)
                        {
                            // codeStatements.Append(this.tab + "List<" + returnType + "> temp" + element.Name.LocalName + "List = new List<" + returnType + ">();\n");
                            this.AddVariableDecleration("temp" + element.Name.LocalName + "List", this.tab + "List<" + returnType + "> temp" + element.Name.LocalName + "List = new List<" + returnType + ">();");
                            listElements.Add(element.Name.LocalName, 1);
                        }
                        var counter = listElements[element.Name.LocalName];
                        var localVar = "temp" + element.Name.LocalName + counter;
                        if (!IsBasicReturnType(returnType))
                        {
                            //codeStatements.Append(returnType + " " + localVar + " = new " + returnType + "();\n");
                            this.AddVariableDecleration(localVar, returnType + " " + localVar + " = new " + returnType + "();");
                        }
                        else
                        {
                            //codeStatements.Append(returnType + " " + localVar + ";\n");
                            this.AddVariableDecleration(localVar, returnType + " " + localVar + ";");
                        }
                        
                        codeStatements.Append(this.Build(element.Nodes(), "temp" + element.Name.LocalName + counter));
                        codeStatements.Append("temp" + element.Name.LocalName + "List.Add(temp" + element.Name.LocalName + counter + ");\n");
                        if (this.IsTheLastElementOfTheList(element, inputNodes, counter))
                        {
                            codeStatements.Append(variableReference + " = temp" + element.Name.LocalName + "List.ToArray();\n");
                        }

                        listElements[element.Name.LocalName] = counter + 1;
                    }
                    else if (returnType == null)
                    {
                        //TODO ugly thing need to find a way to get the real type
                        if (string.IsNullOrEmpty(parent))
                        {
                            codeStatements.Append("object ");
                        }

                        codeStatements.Append(variableReference + " = null;\n");
                    }
                    else
                    {
                        // intialise the variable first
                        this.InitialiseVariable(packageName, parent, returnType, variableReference);

                        // add the value
                        if (string.IsNullOrEmpty(parent))
                        {
                            codeStatements.Append(this.tab);
                            codeStatements.Append(this.Build(element.Nodes(), VariableHelper.ToSafeType(element.Name.LocalName)));
                        }
                        else
                        {
                            codeStatements.Append(this.tab);
                            var nextParentName = parent + "." + element.Name.LocalName;
                            codeStatements.Append(this.Build(element.Nodes(), nextParentName));
                        }
                    }
                    //if (isAlistElement)
                    //{
                        //recursive call to get the value
                        //codeStatements.Append(variableReference + ".Add(" + this.Build(element.Nodes(), parent) + ");\n");

                    //}

                }
                else if(element != null)
                {
                    if (element.Name.LocalName == "value-of")
                    {
                        codeStatements.Append(this.ReturnValue(element, parent));
                    }
                    else if (element.Name.LocalName == "copy-of")
                    {
                        codeStatements.Append(this.ReturnValue(element, parent));
                    }
                    else if (element.Name.LocalName == "attribute")
                    {
                        codeStatements.Append(this.BuildAttribute(element, parent));
                    }
                    else if (element.Name.LocalName == "if")
                    {
                        codeStatements.Append(this.ManageConditionTag(element, parent, true));
                    }
                    else if (element.Name.LocalName == "choose")
                    {
                        codeStatements.Append(this.Build(element.Nodes(), parent));
                    }
                    else if (element.Name.LocalName == "when")
                    {
                        codeStatements.Append(this.ManageConditionTag(element, parent, true));
                    }
                    else if (element.Name.LocalName == "otherwise")
                    {
                        codeStatements.Append(this.ManageConditionTag(element, parent, false));
                    }
                    else if (element.Name.LocalName == "for-each")
                    {
                        codeStatements.Append(this.ManageIterationTag(element, parent));
                    }
                    else if (element.Name.LocalName == "variable")
                    {
                        string returnType = DefineReturnType(parent, element);
                        codeStatements.Append(this.tab);
                        codeStatements.Append(returnType + " ");
                        if(element.Attribute("select") == null)
                        {
                            codeStatements.Append(this.Build(element.Nodes(), VariableHelper.ToSafeType(element.Attribute("name").Value)));
                        }
                        else
                        {
                            codeStatements.Append(ReturnValue(element, VariableHelper.ToSafeType(element.Attribute("name").Value)));
                        }
                    }
                }
            }

            return codeStatements;
        }

        private void InitialiseVariable(
            string packageName,
            string parent,
            string returnType,
            string variableReference)
        {
            var codeStatements = new StringBuilder();

            if (string.IsNullOrEmpty(parent))
            {
                codeStatements.Append(packageName + returnType + " ");
                if (!IsBasicReturnType(returnType))
                {
                    codeStatements.Append(variableReference + " = new " + packageName + returnType + "();");
                }
                else
                {
                    codeStatements.Append(variableReference + ";");
                }
            }
            else
            {
                if (!IsBasicReturnType(returnType))
                {
                    codeStatements.Append(variableReference + " = new " + packageName + returnType + "();");
                }
            }

            this.AddVariableDecleration(variableReference,codeStatements.ToString());
        }

        private string BuildAttribute(XElement element, string parent)
        {
            //return this.xpathBuilder.Build(element.Attribute("select").Value);
            string elementName = VariableHelper.ToSafeType(element.Attribute("name").Value);
            var assignationString = parent + "." + elementName;

            if (elementName != "xsi:nil")
            {
                return assignationString + this.Build(element.Nodes(), null);
            }

            return parent + " = " + "null;\n";
        }

        public StringBuilder ManageIterationTag(XElement element, string parent)
        {
            var codeStatements = new StringBuilder();
            var returnType = DefineReturnType(parent, element);
            var variableReference = this.DefineVariableReference((XElement)element.FirstNode, null);
            var variableListReference = this.DefineVariableReference((XElement)element.FirstNode, parent) + "s";
            codeStatements.Append(this.tab + variableListReference + " = new List<" + returnType + ">();\n");
            codeStatements.Append(this.tab + "foreach (var item in " + this.ReturnForEachValue(element) + ")\n{\n");
            this.tab.Increment();
            codeStatements.Append(this.Build(element.Nodes(), null));
            codeStatements.Append(this.tab + variableListReference + ".Add(" + variableReference + ");\n");
            codeStatements.Append(this.tab.Decrement() + "}\n");
            return codeStatements;
        }

        public StringBuilder ManageConditionTag(XElement element, string parent, bool isIfCondition)
        {
            var codeStatements = new StringBuilder();
            var test = isIfCondition ? "if (" + this.ReturnCondition(element) + ")\n{\n" : "else\n{\n";
            codeStatements.Append(test);
            this.tab.Increment();
            codeStatements.Append(this.Build(element.Nodes(), parent));
            codeStatements.Append(this.tab.Decrement() + "}\n");
            return codeStatements;
        }

        public string ReturnValue(XElement element, string parent)
        {
            return parent + " = " + this.xpathBuilder.Build(element.Attribute("select").Value) + ";\n";
        }

        public string ReturnForEachValue(XElement element)
        {
            return this.xpathBuilder.Build(element.Attribute("select").Value);
        }

        public string ReturnCondition(XElement element)
        {
            return this.xpathBuilder.Build(element.Attribute("test").Value);
        }

        public static bool IsBasicReturnType(string returnType)
        {
            switch (returnType)
            {
                case CSharpTypeConstant.SystemString:
                    return true;
                case "Double?":
                    return true;
                case "double?":
                    return true;
                case "Int?":
                    return true;
                case "Int32?":
                    return true;
                case "bool?":
                    return true;
                case "string":
                    return true;
                case "DateTime?":
                    return true;
                default:
                    return false;
            }
        }

        public static string DefineReturnType(string parent, XElement inputedElement)
        {
            if (inputedElement.Attribute(XmlnsConstant.xsiNameSpace + "nil") != null && inputedElement.Attribute(XmlnsConstant.xsiNameSpace + "nil").Value == "true")
            {
                return null;
            }

            var elementTypes = new List<string>();
            var nodes = new List<XNode> { inputedElement };
            RetrieveAllTypeInTheElement(nodes, elementTypes);
            string returnType;
            if (elementTypes.Count > 1 && IsBasicReturnType(elementTypes[1])) //if (elementTypes.Count == 2)
            {
                returnType = elementTypes[1];
            }
            else
            {
                returnType = elementTypes[0];
            }

            returnType = VariableHelper.ToSafeType(parent, returnType);

            return returnType;
        }

        public string DefineVariableReference(XElement inputedElement, string parent)
        {
            var elementTypes = new List<string>();
            var nodes = new List<XNode> { inputedElement };
            RetrieveAllTypeInTheElement(nodes, elementTypes);
            if (parent == null)
            {
                return VariableHelper.ToSafeType(elementTypes[0]);
            }
            
            return parent + "." + elementTypes[0];
        }

        public bool IsAListElement(XElement inputElement, IEnumerable<XNode> inputNodes)
        {
            int count = (from a in inputNodes
                         where ((XElement)a).Name == inputElement.Name
                         select a).Count();

            if (count > 1)
            {
                return true;
            }
            return false;
        }

        public bool IsTheLastElementOfTheList(XElement inputElement, IEnumerable<XNode> inputNodes, int counter)
        {
            int count = (from a in inputNodes
                where ((XElement)a).Name == inputElement.Name
                select a).Count();

            if (count == counter)
            {
                return true;
            }

            return false;
        }

        private static void RetrieveAllTypeInTheElement(IEnumerable<XNode> inputedElement, List<string> elementTypes)
        {
            
            foreach (XElement item in inputedElement)
            {
                if (!Regex.IsMatch(item.Name.NamespaceName, XmlnsConstant.xslNameSpace))
                {
                    elementTypes.Add(item.Name.LocalName);
                }
                else if (item.Name.LocalName == "value-of")
                {
                    elementTypes.Add(GetTypeFromAttribute(item));
                }
                else if (item.Name.LocalName == "variable")
                {
                    if (item.Attribute("select") != null)
                    {
                        elementTypes.Add(GetTypeFromAttribute(item));
                    }
                    else
                    {
                        elementTypes.Add(item.Attribute("name").Value);
                    }
                }
                else if (item.Name.LocalName == "attribute")
                {
                    elementTypes.Add(item.Attribute("name").Value);
                }

                if (item.HasElements)
                {
                    RetrieveAllTypeInTheElement(item.Nodes(), elementTypes);
                }
            }
        }

        static string GetTypeFromAttribute(XElement item)
        {
            int number = 0;
            if (item.Attribute("select") != null)
            {
                if (item.Attribute("select").Value.Contains("tib:parse-date"))
                {
                    return "DateTime?";
                }
                else if (item.Attribute("select").Value.StartsWith("number("))
                {
                    return "Double?";
                }
                else
                {
                    if (Int32.TryParse(item.Attribute("select").Value, out number))
                    {
                        return "Int32?";
                    }
                    else
                    {
                        return CSharpTypeConstant.SystemString;
                    }
                }
            }
            return String.Empty;
        }

        public static string FormatCorrectlyPackageName(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || IsBasicReturnType(packageName.Remove(packageName.Length-1, 1)) || IsBasicReturnType(packageName))
            {
                return string.Empty;
            }

            if (packageName.EndsWith("."))
            {
                return packageName;
            }

            return packageName + ".";
        }
    }
}

