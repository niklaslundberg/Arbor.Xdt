using System.Collections.Generic;

namespace Arbor.Xdt
{
    public class SetTokenizedAttributeStorage
    {
        public SetTokenizedAttributeStorage() : this(4)
        {
        }

        public SetTokenizedAttributeStorage(int capacity)
        {
            DictionaryList = new List<Dictionary<string, string>>(capacity);
            TokenFormat = string.Concat("$(ReplacableToken_#(",
                SetTokenizedAttributes.ParameterAttribute,
                ")_#(",
                SetTokenizedAttributes.TokenNumber,
                "))");
            EnableTokenizeParameters = false;
            UseXpathToFormParameter = true;
        }

        public List<Dictionary<string, string>> DictionaryList { get; }
        public string TokenFormat { get; }
        public bool EnableTokenizeParameters { get; }
        public bool UseXpathToFormParameter { get; }
    }
}