using System.Globalization;

namespace Arbor.Xdt
{
    internal class InsertAfter : InsertBase
    {
        protected override void Apply()
        {
            SiblingElement.ParentNode.InsertAfter(TransformNode, SiblingElement);

            Log.LogMessage(MessageType.Verbose,
                string.Format(CultureInfo.CurrentCulture,
                    SR.XMLTRANSFORMATION_TransformMessageInsert,
                    TransformNode.Name));
        }
    }
}