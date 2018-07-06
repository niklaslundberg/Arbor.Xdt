using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Xml;

namespace Arbor.Xdt
{
    [Serializable]
    public sealed class XmlNodeException : XmlTransformationException
    {
        private XmlFileInfoDocument document;
        private IXmlLineInfo lineInfo;

        public static Exception Wrap(Exception ex, XmlNode node) {
            if (ex is XmlNodeException) {
                // If this is already an XmlNodeException, then it probably
                // got its node closer to the error, making it more accurate
                return ex;
            }
            else {
                return new XmlNodeException(ex, node);
            }
        }

        public XmlNodeException(Exception innerException, XmlNode node)
            : base(innerException.Message, innerException) {
            this.lineInfo = node as IXmlLineInfo;
            this.document = node.OwnerDocument as XmlFileInfoDocument;
        }

        public XmlNodeException(string message, XmlNode node)
            : base(message) {
            this.lineInfo = node as IXmlLineInfo;
            this.document = node.OwnerDocument as XmlFileInfoDocument;
        }

        public bool HasErrorInfo => lineInfo != null;

        public string FileName => document != null ? document.FileName : null;

        public int LineNumber => lineInfo != null ? lineInfo.LineNumber : 0;

        public int LinePosition => lineInfo != null ? lineInfo.LinePosition : 0;

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("document", document);
            info.AddValue("lineInfo", lineInfo);
        }
    }
}
