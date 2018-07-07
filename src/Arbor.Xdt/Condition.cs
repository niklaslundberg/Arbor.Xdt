namespace Arbor.Xdt
{
    public sealed class Condition : Locator
    {
        protected override string ConstructPredicate()
        {
            EnsureArguments(1, 1);

            return Arguments[0];
        }
    }
}