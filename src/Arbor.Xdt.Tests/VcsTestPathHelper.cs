using System.IO;
using Arbor.Aesculus.Core;
using NCrunch.Framework;

namespace Arbor.Xdt.Tests
{
    public static class VcsTestPathHelper
    {
        public static string FindVcsRootPath(string baseDir = null)
        {
            if (NCrunchEnvironment.NCrunchIsResident())
            {
                var fileInfo = new FileInfo(NCrunchEnvironment.GetOriginalSolutionPath());

                return VcsPathHelper.FindVcsRootPath(fileInfo.Directory?.FullName);
            }

            return VcsPathHelper.FindVcsRootPath(baseDir);
        }
    }
}