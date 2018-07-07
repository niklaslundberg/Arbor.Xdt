using System;

namespace Arbor.Xdt
{
    public sealed class XPath : Locator
    {
        protected override string ParentPath => ConstructPath();

        protected override string ConstructPath()
        {
            EnsureArguments(1, 1);

            string xpath = Arguments[0];
            if (!xpath.StartsWith("/", StringComparison.Ordinal))
            {
                // Relative XPath
                xpath = AppendStep(base.ParentPath, NextStepNodeTest);
                xpath = AppendStep(xpath, Arguments[0]);
                xpath = xpath.Replace("/./", "/");
            }

            return xpath;
        }
    }
}