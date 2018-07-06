using System.Xml;

namespace Arbor.Xdt
{
    internal class XmlNodeContext
    {
        #region private data members

        #endregion

        public XmlNodeContext(XmlNode node)
        {
            Node = node;
        }

        #region data accessors

        public XmlNode Node { get; }

        public bool HasLineInfo => Node is IXmlLineInfo;

        public int LineNumber
        {
            get
            {
                if (Node is IXmlLineInfo lineInfo)
                {
                    return lineInfo.LineNumber;
                }

                return 0;
            }
        }

        public int LinePosition
        {
            get
            {
                if (Node is IXmlLineInfo lineInfo)
                {
                    return lineInfo.LinePosition;
                }

                return 0;
            }
        }

        #endregion
    }
}