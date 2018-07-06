using System.IO;
using Arbor.Aesculus.Core;

namespace Arbor.Xdt.Tests
{
    public static class VcsTestPathHelper
    {
        public static string FindVcsRootPath(string baseDir = null)
        {
            if (NCrunch.Framework.NCrunchEnvironment.NCrunchIsResident())
            {
                var fileInfo = new FileInfo(NCrunch.Framework.NCrunchEnvironment.GetOriginalSolutionPath());

                return VcsPathHelper.FindVcsRootPath(fileInfo.Directory?.FullName);
            }

            return VcsPathHelper.FindVcsRootPath(baseDir);
        }
    }
}