using Chutzpah.Models;
using Chutzpah.Transformers;
using Chutzpah.Wrappers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class TransformProcessorFacts
    {
        private List<TestContext> GetContexts()
        {
            var transforms = new TransformConfig[] 
            {
                new TransformConfig { Name = "testtransform1", Path = "D:\testtransform.out" }
            };

            var settings1 = new ChutzpahTestSettingsFile 
            {
                SettingsFileDirectory = @"D:\directory1",
                Transforms = new List<TransformConfig> { new TransformConfig { Name = "testtransform1", Path = @"D:\testtransform1.out", SettingsFileDirectory = @"D:\directory1" } }
            };

            var settings2 = new ChutzpahTestSettingsFile
            {
                SettingsFileDirectory = @"D:\directory2",
                Transforms = new List<TransformConfig>
                {
                    new TransformConfig { Name = "testtransform1", Path = @"D:\testtransform1.out", SettingsFileDirectory = @"D:\directory2" },
                    new TransformConfig { Name = "testtransform2", Path = @"D:\testtransform2.out", SettingsFileDirectory = @"D:\directory2" }
                }
            };

            return new List<TestContext>
            {
                new TestContext { TestFileSettings = settings1 },
                new TestContext { TestFileSettings = settings1 },
                new TestContext { TestFileSettings = settings2 }
            };
        }

        private TestCaseSummary GetSummary()
        {
            return new TestCaseSummary();
        }

        private class TestableTransformProcessor : Testable<TransformProcessor>
        {
            public Mock<SummaryTransformer> Transformer1 { get; private set; }
            public Mock<SummaryTransformer> Transformer2 { get; private set; }

            public TestableTransformProcessor()
            {
                this.Transformer1 = GetSummaryTransformer("testtransform1");
                this.Transformer2 = GetSummaryTransformer("testtransform2");
                
                this.Mock<IFileSystemWrapper>()
                    .Setup(x => x.IsPathRooted(It.IsAny<string>()))
                    .Returns(true);

                this.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFullPath(It.IsAny<string>()))
                    .Returns((string s) => s);
                
                this.Mock<ISummaryTransformerProvider>()
                    .Setup(x => x.GetTransformers(this.Mock<IFileSystemWrapper>().Object))
                    .Returns(new SummaryTransformer[] 
                    {
                        this.Transformer1.Object,
                        this.Transformer2.Object
                    });
            }

            private Mock<SummaryTransformer> GetSummaryTransformer(string name)
            {
                var mockFilesystem = this.Mock<IFileSystemWrapper>();

                var mock = new Mock<SummaryTransformer>(mockFilesystem.Object);
                mock.SetupGet(x => x.Name).Returns(name);
                mock.Setup(x => x.Transform(It.IsAny<TestCaseSummary>())).Returns("transformer output");

                return mock;
            }
        }

        [Fact]
        public void Runs_transforms_once_per_config_file()
        {
            var processor = new TestableTransformProcessor();
            var contexts = GetContexts();
            var summary = GetSummary();

            processor.ClassUnderTest.ProcessTransforms(contexts, summary);

            processor.Transformer1.Verify(x => x.Transform(summary, @"D:\testtransform1.out"), Times.Exactly(2));
            processor.Transformer2.Verify(x => x.Transform(summary, @"D:\testtransform2.out"), Times.Once());
        }

        [Fact]
        public void Ignores_casing_of_transform_names() 
        {
            var processor = new TestableTransformProcessor();
            var contexts = GetContexts();
            var summary = GetSummary();

            foreach (var context in contexts)
            {
                foreach (var transform in context.TestFileSettings.Transforms)
                {
                    transform.Name = transform.Name.ToUpper();
                }
            }

            processor.ClassUnderTest.ProcessTransforms(contexts, summary);

            processor.Transformer1.Verify(x => x.Transform(summary, @"D:\testtransform1.out"), Times.Exactly(2));
            processor.Transformer2.Verify(x => x.Transform(summary, @"D:\testtransform2.out"), Times.Once());
        }

        [Fact]
        public void Does_not_throw_if_no_transforms_specified()
        {
            var processor = new TestableTransformProcessor();
            var contexts = GetContexts();
            var summary = GetSummary();

            foreach (var context in contexts)
            {
                context.TestFileSettings.Transforms.Clear();
            }

            processor.ClassUnderTest.ProcessTransforms(contexts, summary);
        }

        [Fact]
        public void Calls_only_specified_transforms()
        {
            var processor = new TestableTransformProcessor();
            var contexts = GetContexts();
            var summary = GetSummary();

            // Third context is the only one for Transformer2
            contexts[2].TestFileSettings.Transforms.Clear();

            processor.ClassUnderTest.ProcessTransforms(contexts, summary);

            processor.Transformer1.Verify(x => x.Transform(summary, @"D:\testtransform1.out"), Times.Once());
            processor.Transformer2.Verify(x => x.Transform(summary, @"D:\testtransform2.out"), Times.Never());
        }

        [Fact]
        public void Calls_transforms_with_absolute_paths_when_supplied()
        {
            var processor = new TestableTransformProcessor();
            var contexts = GetContexts();
            var summary = GetSummary();

            processor.ClassUnderTest.ProcessTransforms(contexts, summary);

            processor.Transformer2.Verify(x => x.Transform(summary, @"D:\testtransform2.out"), Times.Once());
        }

        [Fact]
        public void Calls_transforms_relative_to_settings_file_if_path_relative()
        {
            var processor = new TestableTransformProcessor();
            var contexts = GetContexts();
            var summary = GetSummary();

            contexts[2].TestFileSettings.Transforms.Last().Path = "testtransform2.out";
            processor
                .Mock<IFileSystemWrapper>()
                .Setup(x => x.IsPathRooted("testtransform2.out"))
                .Returns(false);

            processor.ClassUnderTest.ProcessTransforms(contexts, summary);

            processor.Transformer2.Verify(x => x.Transform(summary, @"D:\directory2\testtransform2.out"), Times.Once());
        }
    }
}
