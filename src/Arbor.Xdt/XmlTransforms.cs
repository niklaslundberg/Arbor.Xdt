using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Arbor.Xdt
{
    internal static class CommonErrors
    {
        internal static void ExpectNoArguments(XmlTransformationLogger log, string transformName, string argumentString)
        {
            if (!string.IsNullOrEmpty(argumentString))
            {
                log.LogWarning(SR.XMLTRANSFORMATION_TransformDoesNotExpectArguments, transformName);
            }
        }

        internal static void WarnIfMultipleTargets(
            XmlTransformationLogger log,
            string transformName,
            XmlNodeList targetNodes,
            bool applyTransformToAllTargets)
        {
            Debug.Assert(applyTransformToAllTargets == false);

            if (targetNodes.Count > 1)
            {
                log.LogWarning(SR.XMLTRANSFORMATION_TransformOnlyAppliesOnce, transformName);
            }
        }
    }

    internal class Replace : Transform
    {
        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            XmlNode parentNode = TargetNode.ParentNode;
            parentNode?.ReplaceChild(
                TransformNode,
                TargetNode);

            Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageReplace, TargetNode.Name);
        }
    }

    internal class Remove : Transform
    {
        protected override void Apply()
        {
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            RemoveNode();
        }

        protected void RemoveNode()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);

            XmlNode parentNode = TargetNode.ParentNode;
            parentNode?.RemoveChild(TargetNode);

            Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageRemove, TargetNode.Name);
        }
    }

    internal class RemoveAll : Remove
    {
        public RemoveAll()
        {
            ApplyTransformToAllTargetNodes = true;
        }

        protected override void Apply()
        {
            RemoveNode();
        }
    }

    internal class Insert : Transform
    {
        public Insert()
            : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error)
        {
        }

        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);

            TargetNode.AppendChild(TransformNode);

            Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name);
        }
    }

    internal class InsertIfMissing : Insert
    {
        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);
            if (TargetChildNodes == null || TargetChildNodes.Count == 0)
            {
                TargetNode.AppendChild(TransformNode);
                Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name);
            }
        }
    }

    internal abstract class InsertBase : Transform
    {
        private XmlElement _siblingElement;

        internal InsertBase()
            : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error)
        {
        }

        protected XmlElement SiblingElement
        {
            get
            {
                if (_siblingElement == null)
                {
                    if (Arguments == null || Arguments.Count == 0)
                    {
                        throw new XmlTransformationException(string.Format(
                            CultureInfo.CurrentCulture,
                            SR.XMLTRANSFORMATION_InsertMissingArgument,
                            GetType().Name));
                    }

                    if (Arguments.Count > 1)
                    {
                        throw new XmlTransformationException(string.Format(
                            CultureInfo.CurrentCulture,
                            SR.XMLTRANSFORMATION_InsertTooManyArguments,
                            GetType().Name));
                    }

                    string xpath = Arguments[0];
                    XmlNodeList siblings = TargetNode.SelectNodes(xpath);
                    if (siblings.Count == 0)
                    {
                        throw new XmlTransformationException(string.Format(
                            CultureInfo.CurrentCulture,
                            SR.XMLTRANSFORMATION_InsertBadXPath,
                            xpath));
                    }

                    _siblingElement = siblings[0] as XmlElement;
                    if (_siblingElement == null)
                    {
                        throw new XmlTransformationException(string.Format(
                            CultureInfo.CurrentCulture,
                            SR.XMLTRANSFORMATION_InsertBadXPathResult,
                            xpath));
                    }
                }

                return _siblingElement;
            }
        }
    }

    internal class InsertAfter : InsertBase
    {
        protected override void Apply()
        {
            SiblingElement.ParentNode.InsertAfter(TransformNode, SiblingElement);

            Log.LogMessage(MessageType.Verbose,
                string.Format(CultureInfo.CurrentCulture,
                    SR.XMLTRANSFORMATION_TransformMessageInsert,
                    TransformNode.Name));
        }
    }

    internal class InsertBefore : InsertBase
    {
        protected override void Apply()
        {
            SiblingElement.ParentNode.InsertBefore(TransformNode, SiblingElement);

            Log.LogMessage(MessageType.Verbose,
                string.Format(CultureInfo.CurrentCulture,
                    SR.XMLTRANSFORMATION_TransformMessageInsert,
                    TransformNode.Name));
        }
    }

    public class SetAttributes : AttributeTransform
    {
        protected override void Apply()
        {
            foreach (XmlAttribute transformAttribute in TransformAttributes)
            {
                var targetAttribute = TargetNode.Attributes.GetNamedItem(transformAttribute.Name) as XmlAttribute;
                if (targetAttribute != null)
                {
                    targetAttribute.Value = transformAttribute.Value;
                }
                else
                {
                    TargetNode.Attributes.Append((XmlAttribute)transformAttribute.Clone());
                }

                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageSetAttribute,
                    transformAttribute.Name);
            }

            if (TransformAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageSetAttributes,
                    TransformAttributes.Count);
            }
            else
            {
                Log.LogWarning(SR.XMLTRANSFORMATION_TransformMessageNoSetAttributes);
            }
        }
    }

    public class SetTokenizedAttributeStorage
    {
        public SetTokenizedAttributeStorage() : this(4)
        {
        }

        public SetTokenizedAttributeStorage(int capacity)
        {
            DictionaryList = new List<Dictionary<string, string>>(capacity);
            TokenFormat = string.Concat("$(ReplacableToken_#(",
                SetTokenizedAttributes.ParameterAttribute,
                ")_#(",
                SetTokenizedAttributes.TokenNumber,
                "))");
            EnableTokenizeParameters = false;
            UseXpathToFormParameter = true;
        }

        public List<Dictionary<string, string>> DictionaryList { get; }
        public string TokenFormat { get; }
        public bool EnableTokenizeParameters { get; }
        public bool UseXpathToFormParameter { get; }
    }

    /// <summary>
    /// Utility class to Transform the SetAttribute to replace token
    /// 1. if it trigger by the regular TransformXml task, it only replace the $(name) from the parent node
    /// 2. If it trigger by the TokenizedTransformXml task, it replace $(name) then parse the declareation of the parameter
    /// </summary>
    public class SetTokenizedAttributes : AttributeTransform
    {
        public static readonly string Token = "Token";
        public static readonly string TokenNumber = "TokenNumber";
        public static readonly string XPathWithIndex = "XPathWithIndex";
        public static readonly string ParameterAttribute = "Parameter";
        public static readonly string XpathLocator = "XpathLocator";
        public static readonly string XPathWithLocator = "XPathWithLocator";

        private static Regex _sDirRegex;
        private static Regex _sParentAttribRegex;
        private static Regex _sTokenFormatRegex;
        private bool _fInitStorageDictionary;

        private SetTokenizedAttributeStorage _storageDictionary;

        private XmlAttribute _tokenizeValueCurrentXmlAttribute;

        protected SetTokenizedAttributeStorage TransformStorage
        {
            get
            {
                if (_storageDictionary == null && !_fInitStorageDictionary)
                {
                    _storageDictionary = GetService<SetTokenizedAttributeStorage>();
                    _fInitStorageDictionary = true;
                }

                return _storageDictionary;
            }
        }

        // Directory registrory
        internal static Regex DirRegex
        {
            get
            {
                if (_sDirRegex == null)
                {
                    _sDirRegex = new Regex(
                        @"\G\{%(\s*(?<attrname>\w+(?=\W))(\s*(?<equal>=)\s*'(?<attrval>[^']*)'|\s*(?<equal>=)\s*(?<attrval>[^\s%>]*)|(?<equal>)(?<attrval>\s*?)))*\s*?%\}");
                }

                return _sDirRegex;
            }
        }

        internal static Regex ParentAttributeRegex
        {
            get
            {
                if (_sParentAttribRegex == null)
                {
                    _sParentAttribRegex = new Regex(@"\G\$\((?<tagname>[\w:\.]+)\)");
                }

                return _sParentAttribRegex;
            }
        }

        internal static Regex TokenFormatRegex
        {
            get
            {
                if (_sTokenFormatRegex == null)
                {
                    _sTokenFormatRegex = new Regex(@"\G\#\((?<tagname>[\w:\.]+)\)");
                }

                return _sTokenFormatRegex;
            }
        }

        protected override void Apply()
        {
            bool fTokenizeParameter = false;
            SetTokenizedAttributeStorage storage = TransformStorage;
            List<Dictionary<string, string>> parameters = null;

            if (storage != null)
            {
                fTokenizeParameter = storage.EnableTokenizeParameters;
                if (fTokenizeParameter)
                {
                    parameters = storage.DictionaryList;
                }
            }

            foreach (XmlAttribute transformAttribute in TransformAttributes)
            {
                var targetAttribute = TargetNode.Attributes.GetNamedItem(transformAttribute.Name) as XmlAttribute;

                string newValue = TokenizeValue(targetAttribute, transformAttribute, fTokenizeParameter, parameters);

                if (targetAttribute != null)
                {
                    targetAttribute.Value = newValue;
                }
                else
                {
                    var newAttribute = (XmlAttribute)transformAttribute.Clone();
                    newAttribute.Value = newValue;
                    TargetNode.Attributes.Append(newAttribute);
                }

                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageSetAttribute,
                    transformAttribute.Name);
            }

            if (TransformAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageSetAttributes,
                    TransformAttributes.Count);
            }
            else
            {
                Log.LogWarning(SR.XMLTRANSFORMATION_TransformMessageNoSetAttributes);
            }
        }

        protected string GetAttributeValue(string attributeName)
        {
            string dataValue = null;
            var sourceAttribute = TargetNode.Attributes.GetNamedItem(attributeName) as XmlAttribute;
            if (sourceAttribute == null)
            {
                if (string.Compare(attributeName,
                        _tokenizeValueCurrentXmlAttribute.Name,
                        StringComparison.OrdinalIgnoreCase) != 0)
                {
                    // if it is other attributename, we fall back to the current now
                    sourceAttribute = TransformNode.Attributes.GetNamedItem(attributeName) as XmlAttribute;
                }
            }

            if (sourceAttribute != null)
            {
                dataValue = sourceAttribute.Value;
            }

            return dataValue;
        }

        //DirRegex treat single quote differently
        private static string EscapeDirRegexSpecialCharacter(string value, bool escape)
        {
            if (escape)
            {
                return value.Replace("'", "&apos;");
            }

            return value.Replace("&apos;", "'");
        }

        protected static string SubstituteKownValue(
            string transformValue,
            Regex patternRegex,
            string patternPrefix,
            GetValueCallback getValueDelegate)
        {
            int position = 0;
            var matchsExpr = new List<System.Text.RegularExpressions.Match>();
            do
            {
                position = transformValue.IndexOf(patternPrefix, position, StringComparison.OrdinalIgnoreCase);
                if (position > -1)
                {
                    System.Text.RegularExpressions.Match match = patternRegex.Match(transformValue, position);
                    // Add the successful match to collection
                    if (match.Success)
                    {
                        matchsExpr.Add(match);
                        position = match.Index + match.Length;
                    }
                    else
                    {
                        position++;
                    }
                }
            } while (position > -1);

            var strbuilder = new StringBuilder(transformValue.Length);
            if (matchsExpr.Count > 0)
            {
                strbuilder.Remove(0, strbuilder.Length);
                position = 0;
                int index = 0;
                foreach (System.Text.RegularExpressions.Match match in matchsExpr)
                {
                    strbuilder.Append(transformValue.Substring(position, match.Index - position));
                    Capture captureTagName = match.Groups["tagname"];
                    string attributeName = captureTagName.Value;

                    string newValue = getValueDelegate(attributeName);

                    if (newValue != null) // null indicate that the attribute is not exist
                    {
                        strbuilder.Append(newValue);
                    }
                    else
                    {
                        // keep original value
                        strbuilder.Append(match.Value);
                    }

                    position = match.Index + match.Length;
                    index++;
                }

                strbuilder.Append(transformValue.Substring(position));

                transformValue = strbuilder.ToString();
            }

            return transformValue;
        }

        private string GetXPathToAttribute(XmlAttribute xmlAttribute)
        {
            return GetXPathToAttribute(xmlAttribute, null);
        }

        private string GetXPathToAttribute(XmlAttribute xmlAttribute, IList<string> locators)
        {
            string path = string.Empty;
            if (xmlAttribute != null)
            {
                string pathToNode = GetXPathToNode(xmlAttribute.OwnerElement);
                if (!string.IsNullOrEmpty(pathToNode))
                {
                    var identifier = new StringBuilder(256);
                    if (!(locators == null || locators.Count == 0))
                    {
                        foreach (string match in locators)
                        {
                            string val = GetAttributeValue(match);
                            if (!string.IsNullOrEmpty(val))
                            {
                                if (identifier.Length != 0)
                                {
                                    identifier.Append(" and ");
                                }

                                identifier.Append(string.Format(CultureInfo.InvariantCulture,
                                    "@{0}='{1}'",
                                    match,
                                    val));
                            }
                            else
                            {
                                throw new XmlTransformationException(string.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.XMLTRANSFORMATION_MatchAttributeDoesNotExist,
                                    match));
                            }
                        }
                    }

                    if (identifier.Length == 0)
                    {
                        for (int i = 0; i < TargetNodes.Count; i++)
                        {
                            if (TargetNodes[i] == xmlAttribute.OwnerElement)
                            {
                                // Xpath is 1 based
                                identifier.Append((i + 1).ToString(CultureInfo.InvariantCulture));
                                break;
                            }
                        }
                    }

                    pathToNode = string.Concat(pathToNode, "[", identifier.ToString(), "]");
                }

                path = string.Concat(pathToNode, "/@", xmlAttribute.Name);
            }

            return path;
        }

        private string GetXPathToNode(XmlNode xmlNode)
        {
            if (xmlNode == null || xmlNode.NodeType == XmlNodeType.Document)
            {
                return null;
            }

            string parentPath = GetXPathToNode(xmlNode.ParentNode);
            return string.Concat(parentPath, "/", xmlNode.Name);
        }

        private string TokenizeValue(
            XmlAttribute targetAttribute,
            XmlAttribute transformAttribute,
            bool fTokenizeParameter,
            List<Dictionary<string, string>> parameters)
        {
            Debug.Assert(!fTokenizeParameter || parameters != null);

            _tokenizeValueCurrentXmlAttribute = transformAttribute;
            string transformValue = transformAttribute.Value;
            string xpath = GetXPathToAttribute(targetAttribute);

            //subsitute the know value first in the transformAttribute
            transformValue = SubstituteKownValue(transformValue,
                ParentAttributeRegex,
                "$(",
                key => EscapeDirRegexSpecialCharacter(GetAttributeValue(key), true));

            // then use the directive to parse the value. --- if TokenizeParameterize is enable
            if (fTokenizeParameter)
            {
                int position = 0;
                var strbuilder = new StringBuilder(transformValue.Length);
                position = 0;
                var matchs = new List<System.Text.RegularExpressions.Match>();

                do
                {
                    position = transformValue.IndexOf("{%", position, StringComparison.OrdinalIgnoreCase);
                    if (position > -1)
                    {
                        System.Text.RegularExpressions.Match match = DirRegex.Match(transformValue, position);
                        // Add the successful match to collection
                        if (match.Success)
                        {
                            matchs.Add(match);
                            position = match.Index + match.Length;
                        }
                        else
                        {
                            position++;
                        }
                    }
                } while (position > -1);

                if (matchs.Count > 0)
                {
                    strbuilder.Remove(0, strbuilder.Length);
                    position = 0;
                    int index = 0;

                    foreach (System.Text.RegularExpressions.Match match in matchs)
                    {
                        strbuilder.Append(transformValue.Substring(position, match.Index - position));
                        CaptureCollection attrnames = match.Groups["attrname"].Captures;
                        if (attrnames.Count > 0)
                        {
                            CaptureCollection attrvalues =
                                match.Groups["attrval"].Captures;
                            var paramDictionary = new Dictionary<string, string>(4, StringComparer.OrdinalIgnoreCase);

                            paramDictionary[XPathWithIndex] = xpath;
                            paramDictionary[TokenNumber] =
                                index.ToString(CultureInfo.InvariantCulture);

                            // Get the key-value pare of the in the tranform form
                            for (int i = 0; i < attrnames.Count; i++)
                            {
                                string name = attrnames[i].Value;
                                string val = null;
                                if (i < attrvalues.Count)
                                {
                                    val = EscapeDirRegexSpecialCharacter(attrvalues[i].Value, false);
                                }

                                paramDictionary[name] = val;
                            }

                            //Identify the Token format
                            string strTokenFormat = null;
                            if (!paramDictionary.TryGetValue(Token, out strTokenFormat))
                            {
                                strTokenFormat = _storageDictionary.TokenFormat;
                            }

                            if (!string.IsNullOrEmpty(strTokenFormat))
                            {
                                paramDictionary[Token] = strTokenFormat;
                            }

                            // Second translation of #() -- replace with the existing Parameters
                            int count = paramDictionary.Count;
                            var keys = new string[count];
                            paramDictionary.Keys.CopyTo(keys, 0);
                            for (int i = 0; i < count; i++)
                            {
                                // if token format contain the #(),we replace with the known value such that it is unique identify
                                // for example, intokenizeTransformXml.cs, default token format is
                                // string.Concat("$(ReplacableToken_#(", SetTokenizedAttributes.ParameterAttribute, ")_#(", SetTokenizedAttributes.TokenNumber, "))");
                                // which ParameterAttribute will be translate to parameterDictionary["parameter"} and TokenNumber will be translate to parameter
                                // parameterDictionary["TokenNumber"]
                                string keyindex = keys[i];
                                string val = paramDictionary[keyindex];
                                string newVal = SubstituteKownValue(val,
                                    TokenFormatRegex,
                                    "#(",
                                    delegate(string key)
                                    {
                                        return paramDictionary.ContainsKey(key) ? paramDictionary[key] : null;
                                    });

                                paramDictionary[keyindex] = newVal;
                            }

                            if (paramDictionary.TryGetValue(Token, out strTokenFormat))
                            {
                                // Replace with token
                                strbuilder.Append(strTokenFormat);
                            }

                            string attributeLocator;
                            if (paramDictionary.TryGetValue(XpathLocator, out attributeLocator) &&
                                !string.IsNullOrEmpty(attributeLocator))
                            {
                                IList<string> locators = XmlArgumentUtility.SplitArguments(attributeLocator);
                                string xpathwithlocator = GetXPathToAttribute(targetAttribute, locators);
                                if (!string.IsNullOrEmpty(xpathwithlocator))
                                {
                                    paramDictionary[XPathWithLocator] = xpathwithlocator;
                                }
                            }

                            parameters.Add(paramDictionary);
                        }

                        position = match.Index + match.Length;
                        index++;
                    }

                    strbuilder.Append(transformValue.Substring(position));
                    transformValue = strbuilder.ToString();
                }
            }

            return transformValue;
        }

        protected delegate string GetValueCallback(string key);
    }

    public class RemoveAttributes : AttributeTransform
    {
        protected override void Apply()
        {
            foreach (XmlAttribute attribute in TargetAttributes)
            {
                TargetNode.Attributes.Remove(attribute);

                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageRemoveAttribute,
                    attribute.Name);
            }

            if (TargetAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageRemoveAttributes,
                    TargetAttributes.Count);
            }
            else
            {
                Log.LogWarning(TargetNode, SR.XMLTRANSFORMATION_TransformMessageNoRemoveAttributes);
            }
        }
    }
}