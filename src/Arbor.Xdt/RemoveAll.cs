namespace Arbor.Xdt
{
    internal class RemoveAll : Remove
    {
        public RemoveAll()
        {
            ApplyTransformToAllTargetNodes = true;
        }

        protected override void Apply()
        {
            RemoveNode();
        }
    }
}