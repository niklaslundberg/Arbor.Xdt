using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Arbor.Xdt
{
    internal class XmlNodeContext
    {
        #region private data members
        private XmlNode node;
        #endregion

        public XmlNodeContext(XmlNode node) {
            this.node = node;
        }

        #region data accessors
        public XmlNode Node => node;

        public bool HasLineInfo => node is IXmlLineInfo;

        public int LineNumber {
            get {
                var lineInfo = node as IXmlLineInfo;
                if (lineInfo != null) {
                    return lineInfo.LineNumber;
                }
                else {
                    return 0;
                }
            }
        }

        public int LinePosition {
            get {
                var lineInfo = node as IXmlLineInfo;
                if (lineInfo != null) {
                    return lineInfo.LinePosition;
                }
                else {
                    return 0;
                }
            }
        }
        #endregion
    }
}
