using System;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Transformers;
using Xunit;
using Chutzpah.Wrappers;
using Moq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace Chutzpah.Facts.Library.Transformers
{
    public class NUnit2XmlTransformerFacts
    {
        private IFileSystemWrapper GetFileSystemWrapper()
        {
            return new Mock<IFileSystemWrapper>().Object;
        }

        private static TestCaseSummary BuildTestCaseSummary()
        {
            var summary = new TestCaseSummary();
            var fileSummary = new TestFileSummary("path1"){ TimeTaken = 1500};
            fileSummary.AddTestCase(new TestCase
            {
                ModuleName = "module1",
                TestName = "test1",
                TestResults = new List<TestResult> { new TestResult { Passed = false, Message = "some failure" } },
                TimeTaken = 1000
            });
            fileSummary.AddTestCase(new TestCase
            {
                ModuleName = "module1",
                TestName = "test2",
                TestResults = new List<TestResult> { new TestResult { Passed = true } },
                TimeTaken = 500
            });

            var fileSummary2 = new TestFileSummary("path>2") { TimeTaken = 2000 };
            fileSummary2.AddTestCase(new TestCase
            {
                TestName = "test3",
                TestResults = new List<TestResult> { new TestResult { Passed = true } },
                TimeTaken = 1000
            });
            fileSummary2.AddTestCase(new TestCase
            {
                TestName = "test<4",
                TestResults = new List<TestResult> { new TestResult { Passed = false, Message = "bad<failure" } },
                TimeTaken = 1000
            });

            summary.Append(fileSummary);
            summary.Append(fileSummary2);
            return summary;
        }

        [Fact]
        public void Will_throw_if_test_summary_is_null()
        {
            var transformer = new NUnit2XmlTransformer(GetFileSystemWrapper());

            Exception ex = Record.Exception(() => transformer.Transform(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        private XDocument GetTransformedResults()
        {
            var transformer = new NUnit2XmlTransformer(GetFileSystemWrapper());
            var summary = BuildTestCaseSummary();
            var result = transformer.Transform(summary);

            return XDocument.Parse(result);
        }

        [Fact]
        public void Will_generate_testsuite_with_attributes()
        {
            var document = GetTransformedResults();

            // Doing a string join so that it's easy to figure out what's missing and why.
            // Missspellings for instance. :-)
            // Also only checking one because they should all have the same attributes.
            var attributeNames = String.Join(",", document.Descendants("test-suite").First().Attributes().Select(a => a.Name.LocalName).ToArray());
            Assert.Contains("type", attributeNames);
            Assert.Contains("name", attributeNames);
            Assert.Contains("success", attributeNames);
            Assert.Contains("time", attributeNames);
            Assert.Contains("executed", attributeNames);
            Assert.Contains("result", attributeNames);
        }

        [Fact]
        public void Will_generate_testsuite_for_each_file()
        {
            var document = GetTransformedResults();

            var suites = document.Element("test-results").Elements("test-suite").ToDictionary(ts => ts.Attribute("name").Value, ts => ts);

            Assert.Contains("path1", suites.Keys);
            Assert.Contains("path>2", suites.Keys);

            Assert.Equal("False", suites["path1"].Attribute("success").Value);
            Assert.Equal(1.5.ToString(), suites["path1"].Attribute("time").Value);
            Assert.Equal("True", suites["path1"].Attribute("executed").Value);
            Assert.Equal("Failed", suites["path1"].Attribute("result").Value);

            Assert.Equal("False", suites["path>2"].Attribute("success").Value);
            Assert.Equal(2.ToString(), suites["path>2"].Attribute("time").Value);
            Assert.Equal("True", suites["path>2"].Attribute("executed").Value);
            Assert.Equal("Failed", suites["path>2"].Attribute("result").Value);
        }

        [Fact]
        public void Will_generate_testsuite_for_each_module()
        {
            var document = GetTransformedResults();

            var suites = document.Element("test-results").Descendants("test-suite").ToDictionary(ts => ts.Attribute("name").Value == "" ? "_empty_" : ts.Attribute("name").Value, ts => ts);

            Assert.Contains("module1", suites.Keys);
            Assert.Contains("_empty_", suites.Keys);
        }

        [Fact]
        public void Will_generate_testcase_for_each_test()
        {
            var document = GetTransformedResults();

            var cases = document.Descendants("test-case").ToDictionary(ts => ts.Attribute("name").Value, ts => ts);

            Assert.Contains("test1", cases.Keys);
            Assert.Contains("test2", cases.Keys);
            Assert.Contains("test3", cases.Keys);
            Assert.Contains("test<4", cases.Keys);
        }

        [Fact]
        public void Will_generate_failure_message()
        {
            var document = GetTransformedResults();

            var cases = document.Descendants("test-case").ToDictionary(ts => ts.Attribute("name").Value, ts => ts);

            Assert.Contains("failure", cases["test1"].Elements().Select(e => e.Name).ToArray());
            Assert.Contains("message", cases["test1"].Element("failure").Elements().Select(e => e.Name).ToArray());
            Assert.Contains("stack-trace", cases["test1"].Element("failure").Elements().Select(e => e.Name).ToArray());

            Assert.Equal("some failure", cases["test1"].Element("failure").Element("message").Value);
            Assert.Equal("bad<failure", cases["test<4"].Element("failure").Element("message").Value);
        }

        [Fact]
        public void Will_generate_testcase_with_attributes()
        {
            var document = GetTransformedResults();

            // Doing a string join so that it's easy to figure out what's missing and why.
            // Missspellings for instance. :-)
            // Also only checking one because they should all have the same attributes.
            var attributeNames = String.Join(",", document.Descendants("test-case").First().Attributes().Select(a => a.Name.LocalName).ToArray());
            Assert.Contains("name", attributeNames);
            Assert.Contains("description", attributeNames);
            Assert.Contains("success", attributeNames);
            Assert.Contains("time", attributeNames);
            Assert.Contains("executed", attributeNames);
            Assert.Contains("asserts", attributeNames);
            Assert.Contains("result", attributeNames);
        }
    }
}