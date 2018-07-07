using System.Globalization;

namespace Arbor.Xdt
{
    internal class InsertBefore : InsertBase
    {
        protected override void Apply()
        {
            SiblingElement.ParentNode.InsertBefore(TransformNode, SiblingElement);

            Log.LogMessage(MessageType.Verbose,
                string.Format(CultureInfo.CurrentCulture,
                    SR.XMLTRANSFORMATION_TransformMessageInsert,
                    TransformNode.Name));
        }
    }
}