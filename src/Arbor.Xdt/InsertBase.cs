using System.Globalization;
using System.Xml;

namespace Arbor.Xdt
{
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
}