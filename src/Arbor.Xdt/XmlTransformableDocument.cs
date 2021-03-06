using System.Xml;

namespace Arbor.Xdt
{
    public class XmlTransformableDocument : XmlFileInfoDocument, IXmlOriginalDocumentService
    {
        #region private data members

        private XmlDocument _xmlOriginal;

        #endregion

        #region IXmlOriginalDocumentService Members

        XmlNodeList IXmlOriginalDocumentService.SelectNodes(string xpath, XmlNamespaceManager nsmgr)
        {
            if (_xmlOriginal != null)
            {
                return _xmlOriginal.SelectNodes(xpath, nsmgr);
            }

            return null;
        }

        #endregion

        #region public interface

        public bool IsChanged
        {
            get
            {
                if (_xmlOriginal == null)
                {
                    // No transformation has occurred
                    return false;
                }

                return !IsXmlEqual();
            }
        }

        #endregion

        #region Change support

        internal void OnBeforeChange()
        {
            if (_xmlOriginal == null)
            {
                CloneOriginalDocument();
            }
        }

        internal static void OnAfterChange()
        {
        }

        #endregion

        #region Helper methods

        private void CloneOriginalDocument()
        {
            _xmlOriginal = (XmlDocument)Clone();
        }

        private static bool IsXmlEqual()
        {
            // FUTURE: Write a comparison algorithm to see if xmlLeft and
            // xmlRight are different in any significant way. Until then,
            // assume there's a difference.
            return false;
        }

        #endregion
    }
}