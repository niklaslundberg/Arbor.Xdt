using System.Diagnostics;
using System.Xml;

namespace Arbor.Xdt
{
    internal static class CommonErrors
    {
        internal static void ExpectNoArguments(XmlTransformationLogger log, string transformName, string argumentString)
        {
            if (!string.IsNullOrEmpty(argumentString))
            {
                log.LogWarning(SR.XMLTRANSFORMATION_TransformDoesNotExpectArguments, transformName);
            }
        }

        internal static void WarnIfMultipleTargets(
            XmlTransformationLogger log,
            string transformName,
            XmlNodeList targetNodes,
            bool applyTransformToAllTargets)
        {
            Debug.Assert(!applyTransformToAllTargets);

            if (targetNodes.Count > 1)
            {
                log.LogWarning(SR.XMLTRANSFORMATION_TransformOnlyAppliesOnce, transformName);
            }
        }
    }
}