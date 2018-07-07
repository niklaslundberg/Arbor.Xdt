using System.Globalization;
using System.Xml;

namespace Arbor.Xdt
{
    public sealed class Match : Locator
    {
        protected override string ConstructPredicate()
        {
            EnsureArguments(1);

            string keyPredicate = null;

            foreach (string key in Arguments)
            {
                if (CurrentElement.Attributes.GetNamedItem(key) is XmlAttribute keyAttribute)
                {
                    string keySegment = string.Format(CultureInfo.InvariantCulture,
                        "@{0}='{1}'",
                        keyAttribute.Name,
                        keyAttribute.Value);
                    if (keyPredicate == null)
                    {
                        keyPredicate = keySegment;
                    }
                    else
                    {
                        keyPredicate = string.Concat(keyPredicate, " and ", keySegment);
                    }
                }
                else
                {
                    throw new XmlTransformationException(string.Format(CultureInfo.CurrentCulture,
                        SR.XMLTRANSFORMATION_MatchAttributeDoesNotExist,
                        key));
                }
            }

            return keyPredicate;
        }
    }
}