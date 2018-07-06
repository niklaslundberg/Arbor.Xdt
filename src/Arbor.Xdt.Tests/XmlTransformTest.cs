using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Arbor.Xdt.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.Xdt.Tests
{
    [TestClass]
    public class XmlTransformTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void XmlTransform_Support_WriteToStream()
        {
            string src = CreateATestFile("Web.config", Resources.Web);
            string transformFile = CreateATestFile("Web.Release.config", Resources.Web_Release);
            string destFile = GetTestFilePath("MyWeb.config");

            //execute
            using (var x = new XmlTransformableDocument())
            {
                x.PreserveWhitespace = true;
                x.Load(src);

                using (var transform = new XmlTransformation(transformFile))
                {
                    bool succeed = transform.Apply(x);

                    using (var fsDestFile = new FileStream(destFile, FileMode.OpenOrCreate))
                    {
                        x.Save(fsDestFile);

                        //verify, we have a success transform
                        Assert.AreEqual(true, succeed);

                        //verify, the stream is not closed
                        Assert.AreEqual(true,
                            fsDestFile.CanWrite,
                            "The file stream can not be written. was it closed?");

                        //sanity verify the content is right, (xml was transformed)
                        fsDestFile.Close();
                    }

                    string content = File.ReadAllText(destFile);
                    Assert.IsFalse(content.Contains("debug=\"true\"", StringComparison.Ordinal));

                    var lines = new List<string>(File.ReadLines(destFile));
                    //sanity verify the line format is not lost (otherwsie we will have only one long line)
                    Assert.IsTrue(lines.Count > 10);
                }
            }
        }

        [TestMethod]
        public void XmlTransform_AttibuteFormatting()
        {
            Transform_TestRunner_ExpectSuccess(Resources.AttributeFormating_source,
                Resources.AttributeFormating_transform,
                Resources.AttributeFormating_destination,
                Resources.AttributeFormatting_log);
        }

        [TestMethod]
        public void XmlTransform_TagFormatting()
        {
            Transform_TestRunner_ExpectSuccess(Resources.TagFormatting_source,
                Resources.TagFormatting_transform,
                Resources.TagFormatting_destination,
                Resources.TagFormatting_log);
        }

        [TestMethod]
        public void XmlTransform_HandleEdgeCase()
        {
            //2 edge cases we didn't handle well and then fixed it per customer feedback.
            //    a. '>' in the attribute value
            //    b. element with only one character such as <p>
            Transform_TestRunner_ExpectSuccess(Resources.EdgeCase_source,
                Resources.EdgeCase_transform,
                Resources.EdgeCase_destination,
                Resources.EdgeCase_log);
        }

        [TestMethod]
        public void XmlTransform_ErrorAndWarning()
        {
            Transform_TestRunner_ExpectFail(Resources.WarningsAndErrors_source,
                Resources.WarningsAndErrors_transform,
                Resources.WarningsAndErrors_log);
        }

        private void Transform_TestRunner_ExpectSuccess(
            string source,
            string transform,
            string baseline,
            string expectedLog)
        {
            string src = CreateATestFile("source.config", source);
            string transformFile = CreateATestFile("transform.config", transform);
            string baselineFile = CreateATestFile("baseline.config", baseline);
            string destFile = GetTestFilePath("result.config");
            var logger = new TestTransformationLogger();

            bool succeed;

            using (var x = new XmlTransformableDocument())
            {
                x.PreserveWhitespace = true;
                x.Load(src);

                using (var xmlTransform = new XmlTransformation(transformFile, logger))
                {
                    succeed = xmlTransform.Apply(x);
                    x.Save(destFile);
                }
            }

            //test
            Assert.AreEqual(true, succeed);
            CompareFiles(destFile, baselineFile);
            CompareMultiLines(expectedLog, logger.LogText);
        }

        private void Transform_TestRunner_ExpectFail(string source, string transform, string expectedLog)
        {
            string src = CreateATestFile("source.config", source);
            string transformFile = CreateATestFile("transform.config", transform);
            string destFile = GetTestFilePath("result.config");
            var logger = new TestTransformationLogger();

            bool succeed;
            using (var x = new XmlTransformableDocument())
            {
                x.PreserveWhitespace = true;
                x.Load(src);

                using (var xmlTransform = new XmlTransformation(transformFile, logger))
                {
                    succeed = xmlTransform.Apply(x);
                    x.Save(destFile);
                }
            }

            //test
            Assert.AreEqual(false, succeed);
            CompareMultiLines(expectedLog, logger.LogText);
        }

        private void CompareFiles(string baseLinePath, string resultPath)
        {
            string bsl;
            using (var sr = new StreamReader(baseLinePath))
            {
                bsl = sr.ReadToEnd();
            }

            string result;
            using (var sr = new StreamReader(resultPath))
            {
                result = sr.ReadToEnd();
            }

            CompareMultiLines(bsl, result);
        }

        private void CompareMultiLines(string baseline, string result)
        {
            string[] baseLines = baseline.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            string[] resultLines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < baseLines.Length; i++)
            {
                Assert.AreEqual(baseLines[i],
                    resultLines[i],
                    string.Format(CultureInfo.InvariantCulture, "line {0} at baseline file is not matched", i));
            }
        }

        private string CreateATestFile(string filename, string contents)
        {
            string file = GetTestFilePath(filename);
            File.WriteAllText(file, contents);
            return file;
        }

        private string GetTestFilePath(string filename)
        {
            string rootDir = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "temp");

            string folder = Path.Combine(rootDir, TestContext.TestName);
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, filename);
            return file;
        }
    }
}