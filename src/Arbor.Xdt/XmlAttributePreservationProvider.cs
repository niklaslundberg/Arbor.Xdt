using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Arbor.Xdt
{
    internal class XmlAttributePreservationProvider : IDisposable
    {
        private PositionTrackingTextReader _reader;
        private StreamReader _streamReader;
        private FileStream _fileStream;

        public XmlAttributePreservationProvider(string fileName)
        {
            _fileStream = File.OpenRead(fileName);
            _streamReader = new StreamReader(_fileStream);
            _reader = new PositionTrackingTextReader(_streamReader);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_streamReader != null)
                {
                    _streamReader.Close();
                    _streamReader = null;
                }

                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                if (_fileStream != null)
                {
                    _fileStream.Dispose();
                    _fileStream = null;
                }
            }
        }

        public XmlAttributePreservationDict GetDictAtPosition(int lineNumber, int linePosition)
        {
            if (_reader.ReadToPosition(lineNumber, linePosition))
            {
                Debug.Assert((char)_reader.Peek() == '<');

                var sb = new StringBuilder();
                int character;
                bool inAttribute = false;
                do
                {
                    character = _reader.Read();
                    if (character == '\"')
                    {
                        inAttribute = !inAttribute;
                    }

                    sb.Append((char)character);
                } while (character > 0 && ((char)character != '>' || inAttribute));

                if (character > 0)
                {
                    var dict = new XmlAttributePreservationDict();
                    dict.ReadPreservationInfo(sb.ToString());
                    return dict;
                }
            }

            Debug.Fail("Failed to get preservation info");
            return null;
        }

        public void Close()
        {
            Dispose();
        }

        ~XmlAttributePreservationProvider()
        {
            Dispose(false);
        }
    }
}