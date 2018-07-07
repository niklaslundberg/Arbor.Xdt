namespace Arbor.Xdt
{
    internal sealed class DefaultLocator : Locator
    {
        // Uses all the default behavior

        private static DefaultLocator _instance;

        internal static DefaultLocator Instance => _instance ?? (_instance = new DefaultLocator());
    }
}