using System.Xml;

namespace Arbor.Xdt
{
    public class SetAttributes : AttributeTransform
    {
        protected override void Apply()
        {
            foreach (XmlAttribute transformAttribute in TransformAttributes)
            {
                if (TargetNode.Attributes.GetNamedItem(transformAttribute.Name) is XmlAttribute targetAttribute)
                {
                    targetAttribute.Value = transformAttribute.Value;
                }
                else
                {
                    TargetNode.Attributes.Append((XmlAttribute)transformAttribute.Clone());
                }

                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageSetAttribute,
                    transformAttribute.Name);
            }

            if (TransformAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose,
                    SR.XMLTRANSFORMATION_TransformMessageSetAttributes,
                    TransformAttributes.Count);
            }
            else
            {
                Log.LogWarning(SR.XMLTRANSFORMATION_TransformMessageNoSetAttributes);
            }
        }
    }
}