using Chutzpah.Models;
using Chutzpah.Server.Models;
using Xunit;

namespace Chutzpah.Facts
{
    public class UrlBuilderFacts
    {
        private class TestableUrlBuilder : Testable<UrlBuilder>
        {
            public TestableUrlBuilder()
            {
                Mock<IChutzpahWebServerHost>().SetupAllProperties();
            }

            public TestContext GetContext(
                bool isServerMode = false, 
                string testHarnessDirectory = @"c:\testHarnessDirectory", 
                int port = 123, 
                string rootPath = "C:\rootPath",
                string builtInDependencyDirectory = "__chutzpah__")
            {
                Mock<IFileProbe>()
                    .Setup(x => x.BuiltInDependencyDirectory)
                    .Returns(builtInDependencyDirectory);

                Mock<IChutzpahWebServerHost>().Object.Port = port;
                Mock<IChutzpahWebServerHost>().Object.RootPath = rootPath;

                var context = new TestContext();
                context.TestHarnessDirectory = testHarnessDirectory;
                context.TestFileSettings.Server = new ChutzpahWebServerConfiguration
                {
                    Enabled = isServerMode
                };
                context.WebServerHost = Mock<IChutzpahWebServerHost>().Object;

                return context;
            }
        }

        public class GenerateFileUrl
        {
            [Fact]
            public void Will_build_a_server_path_relative_to_test_harness_directory()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: true, testHarnessDirectory: @"c:\harness");

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"c:\some\path.js");

                Assert.Equal(@"../some/path.js", fileUrl);
            }

            [Fact]
            public void Will_build_a_server_path_fully_qualified_relative_to_root_path()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: true, rootPath: @"c:\root", port: 234);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"c:\root\some\path.js", fullyQualified: true);

                Assert.Equal(@"http://localhost:234/some/path.js", fileUrl);
            }

            [Fact]
            public void Will_build_a_server_path_fully_qualified_relative_to_root_path_which_is_not_part_of_path()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: true, rootPath: @"c:\root", port: 234);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"c:\some\path.js", fullyQualified: true);

                Assert.Equal(@"http://localhost:234/../some/path.js", fileUrl);
            }

            [Fact]
            public void Will_build_server_path_for_built_in_dependency()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: true, rootPath: @"c:\root", port: 234, builtInDependencyDirectory: @"c:\chutzpah\testfiles");

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"c:\chutzpah\testfiles\some\path.js", isBuiltInDependency: true);

                Assert.Equal(@"http://localhost:234/__chutzpah__/some/path.js", fileUrl);
            }

            [Fact]
            public void Will_build_local_file_scheme()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: false);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"c:\some\path.js");

                Assert.Equal(@"file:///c:/some/path.js", fileUrl);
            }

            [Fact]
            public void Will_encode_local_path()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: false);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"c:\c#\path.js");

                Assert.Equal(@"file:///c:/c%23/path.js", fileUrl);
            }


            [Fact]
            public void Will_not_prefix_local_path_starting_with_http()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: false);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, "http://someurl/x.js");

                Assert.Equal("http://someurl/x.js", fileUrl);
            }

            [Fact]
            public void Will_not_prefix_local_path_starting_with_https()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: false);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, "https://someurl/x.js");

                Assert.Equal("https://someurl/x.js", fileUrl);
            }

            [Fact]
            public void Will_not_prefix_local_path_starting_with_file()
            {
                var builder = new TestableUrlBuilder();
                var context = builder.GetContext(isServerMode: false);

                var fileUrl = builder.ClassUnderTest.GenerateFileUrl(context, @"file:///c:/some/path.js");

                Assert.Equal(@"file:///c:/some/path.js", fileUrl);
            }
        }

        public class GetRelativePath
        {
            [Fact]
            public void Will_get_relative_path_between_folders()
            {
                var probe = new TestableUrlBuilder();
                var pathFrom = @"C:\a\b\c\";
                var pathTo = @"C:\a\d\";

                var file = UrlBuilder.GetRelativePath(pathFrom,pathTo);

                Assert.NotNull(file);
                Assert.Equal(@"..\..\d\", file);
            }

            [Fact]
            public void Will_treat_folder_from_as_folder_even_without_trailing_slash()
            {
                var probe = new TestableUrlBuilder();
                var pathFrom = @"C:\a\b\c";
                var pathTo = @"C:\a\d\";

                var file = UrlBuilder.GetRelativePath(pathFrom, pathTo);

                Assert.NotNull(file);
                Assert.Equal(@"..\..\d\", file);
            }

            [Fact]
            public void Will_unescape_path_by_default()
            {
                var probe = new TestableUrlBuilder();
                var pathFrom = @"C:\a\b\c";
                var pathTo = @"C:\a\d%3d\";

                var file = UrlBuilder.GetRelativePath(pathFrom, pathTo);

                Assert.NotNull(file);
                Assert.Equal(@"..\..\d%3d\", file);
            }

            [Fact]
            public void Will_leave_path_escaped_when_asked()
            {
                var probe = new TestableUrlBuilder();
                var pathFrom = @"http://a/b/c";
                var pathTo = @"http://a/d%3d";

                var file = UrlBuilder.GetRelativePath(pathFrom, pathTo, false);

                Assert.NotNull(file);
                Assert.Equal(@"..\..\d%3d", file);
            }
        }

        public class NormalizeFilePath
        {
            [Fact]
            public void Will_put_lower_case_and_use_only_backslashes()
            {
                var probe = new TestableUrlBuilder();
                var path = @"C:\a\B/c";

                var file = UrlBuilder.NormalizeFilePath(path);

                Assert.NotNull(file);
                Assert.Equal(@"c:\a\b\c", file);
            }

        }

        public class NormalizeUrlPath
        {
            [Fact]
            public void Will_use_only_forward_slashes()
            {
                var probe = new TestableUrlBuilder();
                var path = @"http://a\B/c";

                var file = UrlBuilder.NormalizeUrlPath(path);

                Assert.NotNull(file);
                Assert.Equal(@"http://a/B/c", file);
            }

        }
    }
}