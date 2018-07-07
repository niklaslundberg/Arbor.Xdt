using System.Xml;

namespace Arbor.Xdt
{
    internal class Replace : Transform
    {
        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            XmlNode parentNode = TargetNode.ParentNode;
            parentNode?.ReplaceChild(
                TransformNode,
                TargetNode);

            Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageReplace, TargetNode.Name);
        }
    }
}