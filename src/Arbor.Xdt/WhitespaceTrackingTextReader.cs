using System.IO;
using System.Text;

namespace Arbor.Xdt
{
    internal class WhitespaceTrackingTextReader : PositionTrackingTextReader
    {
        private StringBuilder _precedingWhitespace = new StringBuilder();

        public WhitespaceTrackingTextReader(TextReader reader)
            : base(reader)
        {
        }

        public string PrecedingWhitespace => _precedingWhitespace.ToString();

        public override int Read()
        {
            int read = base.Read();

            UpdateWhitespaceTracking(read);

            return read;
        }

        private void UpdateWhitespaceTracking(int character)
        {
            if (char.IsWhiteSpace((char)character))
            {
                AppendWhitespaceCharacter(character);
            }
            else
            {
                ResetWhitespaceString();
            }
        }

        private void AppendWhitespaceCharacter(int character)
        {
            _precedingWhitespace.Append((char)character);
        }

        private void ResetWhitespaceString()
        {
            _precedingWhitespace = new StringBuilder();
        }
    }
}