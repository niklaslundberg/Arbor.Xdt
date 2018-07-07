namespace Arbor.Xdt
{
    internal class InsertIfMissing : Insert
    {
        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);
            if (TargetChildNodes == null || TargetChildNodes.Count == 0)
            {
                TargetNode.AppendChild(TransformNode);
                Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name);
            }
        }
    }
}