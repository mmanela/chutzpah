using Chutzpah.Extensions;
using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Library
{
    public class TestingModeExtensionsFacts
    {
        public class FileBelongsToTestingMode
        {
            [Fact]
            public void Will_return_false_if_file_is_null()
            {
                var result = TestingMode.JavaScript.FileBelongsToTestingMode(null);

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_file_extension_is_unknown()
            {
                var file = "file.bar";

                var result = TestingMode.All.FileBelongsToTestingMode(file);

                Assert.False(result);
            }

            [Fact]
            public void Will_match_file_to_its_testing_mode()
            {
                var file = "file.js";

                var result = TestingMode.JavaScript.FileBelongsToTestingMode(file);

                Assert.True(result);
            }


            [Fact]
            public void Will_match_file_to_all_testing_mode()
            {
                var file = "file.js";

                var result = TestingMode.All.FileBelongsToTestingMode(file);

                Assert.True(result);
            }

            [Fact]
            public void Will_match_non_html_file_to_allexcepthtml_testing_mode()
            {
                var file = "file.js";

                var result = TestingMode.AllExceptHTML.FileBelongsToTestingMode(file);

                Assert.True(result);
            }

            [Fact]
            public void Will_not_match_html_file_to_allexcepthtml_testing_mode()
            {
                var file = "file.html";

                var result = TestingMode.AllExceptHTML.FileBelongsToTestingMode(file);

                Assert.False(result);
            }
        }
    }
}
