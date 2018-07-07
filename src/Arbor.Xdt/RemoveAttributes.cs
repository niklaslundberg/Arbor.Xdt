using System.Xml;

namespace Arbor.Xdt
{
    public class RemoveAttributes : AttributeTransform
    {
        protected override void Apply()
        {
            foreach (XmlAttribute attribute in TargetAttributes)
            {
                TargetNode.Attributes.Remove(attribute);

                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageRemoveAttribute,
                    attribute.Name);
            }

            if (TargetAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageRemoveAttributes,
                    TargetAttributes.Count);
            }
            else
            {
                Log.LogWarning(TargetNode, SR.XMLTRANSFORMATION_TransformMessageNoRemoveAttributes);
            }
        }
    }
}