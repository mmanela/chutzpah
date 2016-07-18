using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Library
{
    public class TestHarnessFacts
    {
        public class ExternalStylesheetFacts
        {
            [Fact]
            public void Will_have_stylesheet_relation()
            {
                var stylesheet = new ExternalStylesheet(new ReferencedFile { Path = "file.css"});
                Assert.Equal("stylesheet", stylesheet.Attributes["rel"]);
            }

            [Fact]
            public void Will_have_css_type()
            {
                var stylesheet = new ExternalStylesheet(new ReferencedFile { Path = "file.css" });
                Assert.Equal("text/css", stylesheet.Attributes["type"]);
            }

            [Fact]
            public void Will_have_href()
            {
                var stylesheet = new ExternalStylesheet(new ReferencedFile {  PathForUseInTestHarness = "file.css" });
                Assert.Equal("file.css", stylesheet.Attributes["href"]);
            }

            [Fact]
            public void Will_have_link_tag()
            {
                var stylesheet = new ExternalStylesheet(new ReferencedFile { Path = "file.css" });
                Assert.Contains("<link", stylesheet.ToString());
            }

            [Fact]
            public void Will_have_combined_start_and_end_tag()
            {
                var stylesheet = new ExternalStylesheet(new ReferencedFile { Path = "file.css" });
                Assert.Contains("/>", stylesheet.ToString());
            }
        }

        public class ShortcutIconFacts
        {
            [Fact]
            public void Will_have_shortcut_icon_relation()
            {
                var icon = new ShortcutIcon(new ReferencedFile { Path = "file.png" });
                Assert.Equal("shortcut icon", icon.Attributes["rel"]);
            }

            [Fact]
            public void Will_have_png_type()
            {
                var icon = new ShortcutIcon(new ReferencedFile { Path = "file.png" });
                Assert.Equal("image/png", icon.Attributes["type"]);
            }

            [Fact]
            public void Will_have_href()
            {
                var icon = new ShortcutIcon(new ReferencedFile { PathForUseInTestHarness = "file.png" });
                Assert.Equal("file.png", icon.Attributes["href"]);
            }

            [Fact]
            public void Will_have_link_tag()
            {
                var icon = new ShortcutIcon(new ReferencedFile { Path = "file.png" });
                Assert.True(icon.ToString().StartsWith("<link"));
            }
        }

        public class ExternalScriptFacts
        {
            [Fact]
            public void Will_have_javascript_type()
            {
                var script = new Script(new ReferencedFile { Path = "file.js" });
                Assert.Equal("text/javascript", script.Attributes["type"]);
            }

            [Fact]
            public void Will_have_src()
            {
                var script = new Script(new ReferencedFile { PathForUseInTestHarness = "file.js" });
                Assert.Equal("file.js", script.Attributes["src"]);
            }


            [Fact]
            public void Will_have_explicit_end_tag_and_no_content()
            {
                var script = new Script(new ReferencedFile { Path = "file.js" });
                Assert.Contains("></script>", script.ToString());
            }
        }

        public class InlineScriptFacts
        {
            [Fact]
            public void Will_have_javascript_type()
            {
                var script = new Script("foo=bar");
                Assert.Equal("text/javascript", script.Attributes["type"]);
            }

            [Fact]
            public void Will_have_no_src_attribute()
            {
                var script = new Script("foo=bar");
                Assert.False(script.Attributes.ContainsKey("src"));
            }

            [Fact]
            public void Will_have_inline_content()
            {
                var script = new Script("foo=bar");
                Assert.Contains(">foo=bar<", script.ToString());
            }
        }
    }
}
