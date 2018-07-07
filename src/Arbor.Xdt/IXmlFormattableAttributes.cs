namespace Arbor.Xdt
{
    internal interface IXmlFormattableAttributes
    {
        string AttributeIndent { get; }

        void FormatAttributes(XmlFormatter formatter);
    }
}