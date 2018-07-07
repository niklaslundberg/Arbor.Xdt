using System.IO;

namespace Arbor.Xdt
{
    internal class PositionTrackingTextReader : TextReader
    {
        private const int NewlineCharacter = '\n';
        private int _characterPosition = 1;
        private readonly TextReader _internalReader;

        private int _lineNumber = 1;
        private int _linePosition = 1;

        public PositionTrackingTextReader(TextReader textReader)
        {
            _internalReader = textReader;
        }

        public override int Read()
        {
            int read = _internalReader.Read();

            UpdatePosition(read);

            return read;
        }

        public override int Peek()
        {
            return _internalReader.Peek();
        }

        public bool ReadToPosition(int lineNumber, int linePosition)
        {
            while (_lineNumber < lineNumber && Peek() != -1)
            {
                ReadLine();
            }

            while (_linePosition < linePosition && Peek() != -1)
            {
                Read();
            }

            return _lineNumber == lineNumber && _linePosition == linePosition;
        }

        public bool ReadToPosition(int characterPosition)
        {
            while (_characterPosition < characterPosition && Peek() != -1)
            {
                Read();
            }

            return _characterPosition == characterPosition;
        }

        private void UpdatePosition(int character)
        {
            if (character == NewlineCharacter)
            {
                _lineNumber++;
                _linePosition = 1;
            }
            else
            {
                _linePosition++;
            }

            _characterPosition++;
        }
    }
}