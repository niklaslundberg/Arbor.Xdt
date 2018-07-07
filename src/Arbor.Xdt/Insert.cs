namespace Arbor.Xdt
{
    internal class Insert : Transform
    {
        public Insert()
            : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error)
        {
        }

        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);

            TargetNode.AppendChild(TransformNode);

            Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name);
        }
    }
}