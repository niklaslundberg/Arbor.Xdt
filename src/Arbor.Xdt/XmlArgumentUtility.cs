using System.Collections.Generic;

namespace Arbor.Xdt
{
    internal static class XmlArgumentUtility
    {
        private static IList<string> RecombineArguments(IList<string> arguments, char separator)
        {
            var combinedArguments = new List<string>();
            string combinedArgument = null;
            int parenCount = 0;

            foreach (string argument in arguments)
            {
                if (combinedArgument == null)
                {
                    combinedArgument = argument;
                }
                else
                {
                    combinedArgument = string.Concat(combinedArgument, separator, argument);
                }

                parenCount += CountParens(argument);
                if (parenCount == 0)
                {
                    combinedArguments.Add(combinedArgument);
                    combinedArgument = null;
                }
            }

            if (combinedArgument != null)
            {
                // mismatched parens, we'll let the caller handle it
                combinedArguments.Add(combinedArgument);
            }

            // If the count didn't change, then nothing was recombined
            // so the array is already right. A new array only needs
            // to be created if the count changed.
            if (arguments.Count != combinedArguments.Count)
            {
                arguments = combinedArguments;
            }

            return arguments;
        }

        private static void TrimStrings(IList<string> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                arguments[i] = arguments[i].Trim();
            }
        }

        private static int CountParens(string str)
        {
            int parenCount = 0;
            foreach (char ch in str)
            {
                switch (ch)
                {
                    case '(':
                        parenCount++;
                        break;
                    case ')':
                        parenCount--;
                        break;
                }
            }

            return parenCount;
        }

        internal static IList<string> SplitArguments(string argumentString)
        {
            // Short circuit: If there are no commas, it's all one argument
            if (argumentString.IndexOf(',') == -1)
            {
                return new[] { argumentString };
            }

            // Split the array
            var arguments = new List<string>();
            arguments.AddRange(argumentString.Split(','));

            // We need to recombine arguments that were split by a comma
            // that's enclosed in parentheses.
            IList<string> iListArguments = RecombineArguments(arguments, ',');

            // Last step, clear the whitespace from each argument
            TrimStrings(iListArguments);

            return iListArguments;
        }
    }
}