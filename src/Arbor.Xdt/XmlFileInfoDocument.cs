using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Arbor.Xdt
{
    public class XmlFileInfoDocument : XmlDocument, IDisposable
    {
        private int _lineNumberOffset;
        private int _linePositionOffset;
        private XmlTextReader _reader;
        private Encoding _textEncoding;

        internal bool HasErrorInfo => _reader != null;

        internal string FileName { get; private set; }

        private int CurrentLineNumber => _reader?.LineNumber + _lineNumberOffset ?? 0;

        private int CurrentLinePosition => _reader?.LinePosition + _linePositionOffset ?? 0;

        private bool FirstLoad { get; set; } = true;

        private XmlAttributePreservationProvider PreservationProvider { get; set; }

        private Encoding TextEncoding
        {
            get
            {
                if (_textEncoding != null)
                {
                    return _textEncoding;
                }

                // Copied from base implementation of XmlDocument
                if (HasChildNodes)
                {
                    if (FirstChild is XmlDeclaration declaration)
                    {
                        string value = declaration.Encoding;
                        if (value.Length > 0)
                        {
                            return Encoding.GetEncoding(value);
                        }
                    }
                }

                return null;
            }
        }

        public override void Load(string filename)
        {
            LoadFromFileName(filename);

            FirstLoad = false;
        }

        public override void Load(XmlReader reader)
        {
            _reader = reader as XmlTextReader;
            if (_reader != null)
            {
                FileName = _reader.BaseURI;
            }

            base.Load(reader);

            if (_reader != null)
            {
                _textEncoding = _reader.Encoding;
            }

            FirstLoad = false;
        }

        public override void Save(string filename)
        {
            using (var fileStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
            {
                XmlWriter xmlWriter = null;

                try
                {
                    if (PreserveWhitespace)
                    {
                        XmlFormatter.Format(this);
                        xmlWriter = new XmlAttributePreservingWriter(fileStream, TextEncoding);
                    }
                    else
                    {
                        xmlWriter = new XmlTextWriter(fileStream, TextEncoding) { Formatting = Formatting.Indented };
                    }

                    WriteTo(xmlWriter);
                }
                finally
                {
                    if (xmlWriter != null)
                    {
                        xmlWriter.Flush();
                        xmlWriter.Close();
                        xmlWriter.Dispose();
                        xmlWriter = null;
                    }
                }
            }
        }

        public override void Save(Stream outStream)
        {
            XmlWriter xmlWriter = null;

            try
            {
                if (PreserveWhitespace)
                {
                    XmlFormatter.Format(this);
                    xmlWriter = new XmlAttributePreservingWriter(outStream, TextEncoding);
                }
                else
                {
                    xmlWriter = new XmlTextWriter(outStream, TextEncoding) { Formatting = Formatting.Indented };
                }

                WriteTo(xmlWriter);
            }
            finally
            {
                if (xmlWriter != null)
                {
                    xmlWriter.Flush();
                    xmlWriter.Close();
                    xmlWriter.Dispose();
                    xmlWriter = null;
                }
            }
        }

        public override XmlElement CreateElement(string prefix, string localName, string namespaceUri)
        {
            if (HasErrorInfo)
            {
                return new XmlFileInfoElement(prefix, localName, namespaceUri, this);
            }

            return base.CreateElement(prefix, localName, namespaceUri);
        }

        public override XmlAttribute CreateAttribute(string prefix, string localName, string namespaceUri)
        {
            if (HasErrorInfo)
            {
                return new XmlFileInfoAttribute(prefix, localName, namespaceUri, this);
            }

            return base.CreateAttribute(prefix, localName, namespaceUri);
        }

        private void LoadFromFileName(string filename)
        {
            FileName = filename;

            StreamReader reader = null;

            try
            {
                if (PreserveWhitespace)
                {
                    PreservationProvider = new XmlAttributePreservationProvider(filename);
                }

                reader = new StreamReader(filename, true);

                LoadFromTextReader(reader);
            }
            finally
            {
                if (PreservationProvider != null)
                {
                    PreservationProvider.Close();
                    PreservationProvider = null;
                }

                reader?.Close();
            }
        }

        private void LoadFromTextReader(TextReader textReader)
        {
            if (textReader is StreamReader streamReader)
            {
                if (streamReader.BaseStream is FileStream fileStream)
                {
                    FileName = fileStream.Name;
                }

                _textEncoding = GetEncodingFromStream(streamReader.BaseStream);
            }

            _reader = new XmlTextReader(FileName, textReader);

            base.Load(_reader);

            if (_textEncoding == null)
            {
                _textEncoding = _reader.Encoding;
            }
        }

        private static Encoding GetEncodingFromStream(Stream stream)
        {
            Encoding encoding = null;
            if (stream.CanSeek)
            {
                var buffer = new byte[3];
                stream.Read(buffer, 0, buffer.Length);

                if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                {
                    encoding = Encoding.UTF8;
                }
                else if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                {
                    encoding = Encoding.BigEndianUnicode;
                }
                else if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                {
                    encoding = Encoding.Unicode;
                }
                else if (buffer[0] == 0x2B && buffer[1] == 0x2F && buffer[2] == 0x76)
                {
                    encoding = Encoding.UTF7;
                }

                // Reset the stream
                stream.Seek(0, SeekOrigin.Begin);
            }

            return encoding;
        }

        private static XmlElement FindContainingElement(XmlNode node)
        {
            while (node != null && !(node is XmlElement))
            {
                node = node.ParentNode;
            }

            return node as XmlElement;
        }

        internal XmlNode CloneNodeFromOtherDocument(XmlNode element)
        {
            XmlTextReader oldReader = _reader;
            string oldFileName = FileName;

            XmlNode clone;
            try
            {
                if (element is IXmlLineInfo lineInfo)
                {
                    _reader = new XmlTextReader(new StringReader(element.OuterXml));

                    _lineNumberOffset = lineInfo.LineNumber - 1;
                    _linePositionOffset = lineInfo.LinePosition - 2;
                    FileName = element.OwnerDocument.BaseURI;

                    clone = ReadNode(_reader);
                }
                else
                {
                    FileName = null;
                    _reader = null;

                    clone = ReadNode(new XmlTextReader(new StringReader(element.OuterXml)));
                }
            }
            finally
            {
                _lineNumberOffset = 0;
                _linePositionOffset = 0;
                FileName = oldFileName;

                _reader = oldReader;
            }

            return clone;
        }

        internal static bool IsNewNode(XmlNode node)
        {
            // The transformation engine will only add elements. Anything
            // else that gets added must be contained by a new element.
            // So to determine what's new, we search up the tree for a new
            // element that contains this node.
            return FindContainingElement(node) is XmlFileInfoElement element && !element.IsOriginal;
        }

        #region XmlElement override

        private class XmlFileInfoElement : XmlElement, IXmlLineInfo, IXmlFormattableAttributes
        {
            private readonly XmlAttributePreservationDict _preservationDict;

            internal XmlFileInfoElement(
                string prefix,
                string localName,
                string namespaceUri,
                XmlFileInfoDocument document)
                : base(prefix, localName, namespaceUri, document)
            {
                LineNumber = document.CurrentLineNumber;
                LinePosition = document.CurrentLinePosition;
                IsOriginal = document.FirstLoad;

                if (document.PreservationProvider != null)
                {
                    _preservationDict = document.PreservationProvider.GetDictAtPosition(LineNumber, LinePosition - 1);
                }

                if (_preservationDict == null)
                {
                    _preservationDict = new XmlAttributePreservationDict();
                }
            }

            public override void WriteTo(XmlWriter w)
            {
                string prefix = Prefix;
                if (!string.IsNullOrEmpty(NamespaceURI))
                {
                    prefix = w.LookupPrefix(NamespaceURI) ?? Prefix;
                }

                w.WriteStartElement(prefix, LocalName, NamespaceURI);

                if (HasAttributes)
                {
                    if (!(w is XmlAttributePreservingWriter preservingWriter) || _preservationDict == null)
                    {
                        WriteAttributesTo(w);
                    }
                    else
                    {
                        WritePreservedAttributesTo(preservingWriter);
                    }
                }

                if (IsEmpty)
                {
                    w.WriteEndElement();
                }
                else
                {
                    WriteContentTo(w);
                    w.WriteFullEndElement();
                }
            }

            private void WriteAttributesTo(XmlWriter w)
            {
                XmlAttributeCollection attrs = Attributes;
                for (int i = 0; i < attrs.Count; i++)
                {
                    XmlAttribute attr = attrs[i];
                    attr.WriteTo(w);
                }
            }

            private void WritePreservedAttributesTo(XmlAttributePreservingWriter preservingWriter)
            {
                _preservationDict.WritePreservedAttributes(preservingWriter, Attributes);
            }

            #region IXmlLineInfo Members

            public bool HasLineInfo()
            {
                return true;
            }

            public int LineNumber { get; }

            public int LinePosition { get; }

            public bool IsOriginal { get; }

            #endregion

            #region IXmlFormattableNode Members

            void IXmlFormattableAttributes.FormatAttributes(XmlFormatter formatter)
            {
                _preservationDict.UpdatePreservationInfo(Attributes, formatter);
            }

            string IXmlFormattableAttributes.AttributeIndent => _preservationDict.GetAttributeNewLineString(null);

            #endregion
        }

        #endregion

        #region XmlAttribute override

        private class XmlFileInfoAttribute : XmlAttribute, IXmlLineInfo
        {
            internal XmlFileInfoAttribute(
                string prefix,
                string localName,
                string namespaceUri,
                XmlFileInfoDocument document)
                : base(prefix, localName, namespaceUri, document)
            {
                LineNumber = document.CurrentLineNumber;
                LinePosition = document.CurrentLinePosition;
            }

            #region IXmlLineInfo Members

            public bool HasLineInfo()
            {
                return true;
            }

            public int LineNumber { get; }

            public int LinePosition { get; }

            #endregion
        }

        #endregion

        #region Dispose Pattern

        protected virtual void Dispose(bool disposing)
        {
            if (_reader != null)
            {
                _reader.Close();
                _reader = null;
            }

            if (PreservationProvider != null)
            {
                PreservationProvider.Close();
                PreservationProvider = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~XmlFileInfoDocument()
        {
            Dispose(false);
        }

        #endregion
    }
}