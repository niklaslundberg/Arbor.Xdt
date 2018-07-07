using System.Xml;

namespace Arbor.Xdt
{
    internal class Remove : Transform
    {
        protected override void Apply()
        {
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            RemoveNode();
        }

        protected void RemoveNode()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);

            XmlNode parentNode = TargetNode.ParentNode;
            parentNode?.RemoveChild(TargetNode);

            Log.LogMessage(MessageType.Verbose, SR.XMLTRANSFORMATION_TransformMessageRemove, TargetNode.Name);
        }
    }
}