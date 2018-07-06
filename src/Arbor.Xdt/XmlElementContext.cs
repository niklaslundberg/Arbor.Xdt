using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace Arbor.Xdt
{
    internal class XmlElementContext : XmlNodeContext
    {
        public XmlElementContext(
            XmlElementContext parent,
            XmlElement element,
            XmlDocument xmlTargetDoc,
            IServiceProvider serviceProvider)
            : base(element)
        {
            _parentContext = parent;
            TargetDocument = xmlTargetDoc;
            _serviceProvider = serviceProvider;
        }

        public T GetService<T>() where T : class
        {
            if (_serviceProvider != null)
            {
                var service = _serviceProvider.GetService(typeof(T)) as T;
                // now it is legal to return service that's null -- due to SetTokenizeAttributeStorage
                //Debug.Assert(service != null, String.Format(CultureInfo.InvariantCulture, "Service provider didn't provide {0}", typeof(ServiceType).Name));
                return service;
            }

            Debug.Fail("No ServiceProvider");
            return null;
        }

        #region private data members

        private XmlElementContext _parentContext;
        private string _xpath;
        private string _parentXPath;

        private IServiceProvider _serviceProvider;

        private XmlNode _transformNodes;
        private XmlNodeList _targetNodes;
        private XmlNodeList _targetParents;

        private XmlAttribute _transformAttribute;
        private XmlAttribute _locatorAttribute;

        private XmlNamespaceManager _namespaceManager;

        #endregion

        #region data accessors

        public XmlElement Element => Node as XmlElement;

        public string XPath
        {
            get
            {
                if (_xpath == null)
                {
                    _xpath = ConstructXPath();
                }

                return _xpath;
            }
        }

        public string ParentXPath
        {
            get
            {
                if (_parentXPath == null)
                {
                    _parentXPath = ConstructParentXPath();
                }

                return _parentXPath;
            }
        }

        public Transform ConstructTransform(out string argumentString)
        {
            try
            {
                return CreateObjectFromAttribute<Transform>(out argumentString, out _transformAttribute);
            }
            catch (Exception ex)
            {
                throw WrapException(ex);
            }
        }

        public int TransformLineNumber
        {
            get
            {
                var lineInfo = _transformAttribute as IXmlLineInfo;
                if (lineInfo != null)
                {
                    return lineInfo.LineNumber;
                }

                return LineNumber;
            }
        }

        public int TransformLinePosition
        {
            get
            {
                var lineInfo = _transformAttribute as IXmlLineInfo;
                if (lineInfo != null)
                {
                    return lineInfo.LinePosition;
                }

                return LinePosition;
            }
        }

        public XmlAttribute TransformAttribute => _transformAttribute;

        public XmlAttribute LocatorAttribute => _locatorAttribute;

        #endregion

        #region XPath construction

        private string ConstructXPath()
        {
            try
            {
                string argumentString;
                string parentPath = _parentContext == null ? string.Empty : _parentContext.XPath;

                Locator locator = CreateLocator(out argumentString);

                return locator.ConstructPath(parentPath, this, argumentString);
            }
            catch (Exception ex)
            {
                throw WrapException(ex);
            }
        }

        private string ConstructParentXPath()
        {
            try
            {
                string argumentString;
                string parentPath = _parentContext == null ? string.Empty : _parentContext.XPath;

                Locator locator = CreateLocator(out argumentString);

                return locator.ConstructParentPath(parentPath, this, argumentString);
            }
            catch (Exception ex)
            {
                throw WrapException(ex);
            }
        }

        private Locator CreateLocator(out string argumentString)
        {
            var locator = CreateObjectFromAttribute<Locator>(out argumentString, out _locatorAttribute);
            if (locator == null)
            {
                argumentString = null;
                //avoid using singleton of "DefaultLocator.Instance", so unit tests can run parallel
                locator = new DefaultLocator();
            }

            return locator;
        }

        #endregion

        #region Context information

        internal XmlNode TransformNode
        {
            get
            {
                if (_transformNodes == null)
                {
                    _transformNodes = CreateCloneInTargetDocument(Element);
                }

                return _transformNodes;
            }
        }

        internal XmlNodeList TargetNodes
        {
            get
            {
                if (_targetNodes == null)
                {
                    _targetNodes = GetTargetNodes(XPath);
                }

                return _targetNodes;
            }
        }

        internal XmlNodeList TargetParents
        {
            get
            {
                if (_targetParents == null && _parentContext != null)
                {
                    _targetParents = GetTargetNodes(ParentXPath);
                }

                return _targetParents;
            }
        }

        #endregion

        #region Node helpers

        private XmlDocument TargetDocument { get; }

        private XmlNode CreateCloneInTargetDocument(XmlNode sourceNode)
        {
            var infoDocument = TargetDocument as XmlFileInfoDocument;
            XmlNode clonedNode;

            if (infoDocument != null)
            {
                clonedNode = infoDocument.CloneNodeFromOtherDocument(sourceNode);
            }
            else
            {
                XmlReader reader = new XmlTextReader(new StringReader(sourceNode.OuterXml));
                clonedNode = TargetDocument.ReadNode(reader);
            }

            ScrubTransformAttributesAndNamespaces(clonedNode);

            return clonedNode;
        }

        private void ScrubTransformAttributesAndNamespaces(XmlNode node)
        {
            if (node.Attributes != null)
            {
                var attributesToRemove = new List<XmlAttribute>();
                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute.NamespaceURI == XmlTransformation.TransformNamespace)
                    {
                        attributesToRemove.Add(attribute);
                    }
                    else if (attribute.Prefix.Equals("xmlns", StringComparison.Ordinal) || attribute.Name.Equals("xmlns", StringComparison.Ordinal))
                    {
                        attributesToRemove.Add(attribute);
                    }
                    else
                    {
                        attribute.Prefix = null;
                    }
                }

                foreach (XmlAttribute attributeToRemove in attributesToRemove)
                {
                    node.Attributes.Remove(attributeToRemove);
                }
            }

            // Do the same recursively for child nodes
            foreach (XmlNode childNode in node.ChildNodes)
            {
                ScrubTransformAttributesAndNamespaces(childNode);
            }
        }

        private XmlNodeList GetTargetNodes(string xpath)
        {
            XmlNamespaceManager mgr = GetNamespaceManager();
            return TargetDocument.SelectNodes(xpath, mgr);
        }

        private Exception WrapException(Exception ex)
        {
            return XmlNodeException.Wrap(ex, Element);
        }

        private Exception WrapException(Exception ex, XmlNode node)
        {
            return XmlNodeException.Wrap(ex, node);
        }

        private XmlNamespaceManager GetNamespaceManager()
        {
            if (_namespaceManager == null)
            {
                XmlNodeList localNamespaces = Element.SelectNodes("namespace::*");

                if (localNamespaces.Count > 0)
                {
                    _namespaceManager = new XmlNamespaceManager(Element.OwnerDocument.NameTable);

                    foreach (XmlAttribute nsAttribute in localNamespaces)
                    {
                        string prefix = string.Empty;
                        int index = nsAttribute.Name.IndexOf(':');
                        if (index >= 0)
                        {
                            prefix = nsAttribute.Name.Substring(index + 1);
                        }
                        else
                        {
                            prefix = "_defaultNamespace";
                        }

                        _namespaceManager.AddNamespace(prefix, nsAttribute.Value);
                    }
                }
                else
                {
                    _namespaceManager = new XmlNamespaceManager(GetParentNameTable());
                }
            }

            return _namespaceManager;
        }

        private XmlNameTable GetParentNameTable()
        {
            if (_parentContext == null)
            {
                return Element.OwnerDocument.NameTable;
            }

            return _parentContext.GetNamespaceManager().NameTable;
        }

        #endregion

        #region Named object creation

        private static Regex _nameAndArgumentsRegex;

        private static Regex NameAndArgumentsRegex
        {
            get
            {
                return _nameAndArgumentsRegex ?? (_nameAndArgumentsRegex = new Regex(
                           @"\A\s*(?<name>\w+)(\s*\((?<arguments>.*)\))?\s*\Z",
                           RegexOptions.Compiled | RegexOptions.Singleline));
            }
        }

        private string ParseNameAndArguments(string name, out string arguments)
        {
            arguments = null;

            System.Text.RegularExpressions.Match match = NameAndArgumentsRegex.Match(name);
            if (match.Success)
            {
                if (match.Groups["arguments"].Success)
                {
                    CaptureCollection argumentCaptures = match.Groups["arguments"].Captures;
                    if (argumentCaptures.Count == 1 && !string.IsNullOrEmpty(argumentCaptures[0].Value))
                    {
                        arguments = argumentCaptures[0].Value;
                    }
                }

                return match.Groups["name"].Captures[0].Value;
            }

            throw new XmlTransformationException(SR.XMLTRANSFORMATION_BadAttributeValue);
        }

        private TObjectType CreateObjectFromAttribute<TObjectType>(
            out string argumentString,
            out XmlAttribute objectAttribute) where TObjectType : class
        {
            objectAttribute =
                Element.Attributes.GetNamedItem(typeof(TObjectType).Name, XmlTransformation.TransformNamespace) as
                    XmlAttribute;
            try
            {
                if (objectAttribute != null)
                {
                    string typeName = ParseNameAndArguments(objectAttribute.Value, out argumentString);
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        var factory = GetService<NamedTypeFactory>();
                        return factory.Construct<TObjectType>(typeName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw WrapException(ex, objectAttribute);
            }

            argumentString = null;
            return null;
        }

        #endregion

        #region Error reporting helpers

        internal bool HasTargetNode(out XmlElementContext failedContext, out bool existedInOriginal)
        {
            failedContext = null;
            existedInOriginal = false;

            if (TargetNodes.Count == 0)
            {
                failedContext = this;
                while (failedContext._parentContext != null &&
                       failedContext._parentContext.TargetNodes.Count == 0)
                {
                    failedContext = failedContext._parentContext;
                }

                existedInOriginal = ExistedInOriginal(failedContext.XPath);
                return false;
            }

            return true;
        }

        internal bool HasTargetParent(out XmlElementContext failedContext, out bool existedInOriginal)
        {
            failedContext = null;
            existedInOriginal = false;

            if (TargetParents.Count == 0)
            {
                failedContext = this;
                while (failedContext._parentContext != null &&
                       !string.IsNullOrEmpty(failedContext._parentContext.ParentXPath) &&
                       failedContext._parentContext.TargetParents.Count == 0)
                {
                    failedContext = failedContext._parentContext;
                }

                existedInOriginal = ExistedInOriginal(failedContext.XPath);
                return false;
            }

            return true;
        }

        private bool ExistedInOriginal(string xpath)
        {
            var service = GetService<IXmlOriginalDocumentService>();
            if (service != null)
            {
                XmlNodeList nodeList = service.SelectNodes(xpath, GetNamespaceManager());
                if (nodeList != null && nodeList.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}