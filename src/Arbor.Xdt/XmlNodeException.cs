using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml;

namespace Arbor.Xdt
{
    [Serializable]
    public sealed class XmlNodeException : XmlTransformationException
    {
        private XmlFileInfoDocument _document;
        private IXmlLineInfo _lineInfo;

        public XmlNodeException(Exception innerException, XmlNode node)
            : base(innerException.Message, innerException)
        {
            _lineInfo = node as IXmlLineInfo;
            _document = node.OwnerDocument as XmlFileInfoDocument;
        }

        public XmlNodeException(string message, XmlNode node)
            : base(message)
        {
            _lineInfo = node as IXmlLineInfo;
            _document = node.OwnerDocument as XmlFileInfoDocument;
        }

        public bool HasErrorInfo => _lineInfo != null;

        public string FileName => _document != null ? _document.FileName : null;

        public int LineNumber => _lineInfo != null ? _lineInfo.LineNumber : 0;

        public int LinePosition => _lineInfo != null ? _lineInfo.LinePosition : 0;

        public static Exception Wrap(Exception ex, XmlNode node)
        {
            if (ex is XmlNodeException)
            {
                // If this is already an XmlNodeException, then it probably
                // got its node closer to the error, making it more accurate
                return ex;
            }

            return new XmlNodeException(ex, node);
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("document", _document);
            info.AddValue("lineInfo", _lineInfo);
        }
    }
}