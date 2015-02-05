using System;
using Chutzpah.FileProcessors;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts.Library
{

    public class MochaLineNumberProcessorFacts
    {
        private class TestableMochaLineNumberProcessor : Testable<MochaLineNumberProcessor>
        {
            public TestableMochaLineNumberProcessor()
            {
            }
        }

        public class Process
        {
            [Fact]
            public void Will_get_skip_if_file_is_not_under_test()
            {
                var processor = new TestableMochaLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = false, Path = "path" };

                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor.Mock<IFileSystemWrapper>().Verify(x => x.GetLines(It.IsAny<string>()), Times.Never());
            }

            [Fact]
            public void Will_get_line_numbers_for_bdd_javascript_tests()
            {
                var lines = new[] 
                {
                    "//js file",
                    "describe ('module1', function(){",
                    "  it('test1', function(){});",
                    "    it('test2', function(){});",
                    "});"
                };

                var positions = new[]
                {
                    new FilePosition(3, 7, "someTest1"),
                    new FilePosition(4, 9, "someTest2")
                };

                TestPositions(false, lines, positions);
            }

            [Fact]
            public void Will_get_line_numbers_for_tdd_javascript_tests()
            {
                var lines = new[] 
                {
                    "//js file",
                    "suite ('module1', function(){",
                    "  test('test1', function(){});",
                    "    test('test2', function(){});",
                    "});"
                };

                var positions = new[]
                {
                    new FilePosition(3, 9, "someTest1"),
                    new FilePosition(4, 11, "someTest2")
                };

                TestPositions(false, lines, positions);
            }

            [Fact]
            public void Will_get_line_numbers_for_qunit_javascript_tests()
            {
                var lines = new[] 
                {
                    "//js file",
                    "suite ('module1');",
                    "  test('test1', function(){});",
                    "    test('test2', function(){});"
                };

                var positions = new[]
                {
                    new FilePosition(3, 9, "someTest1"),
                    new FilePosition(4, 11, "someTest2")
                };

                TestPositions(false, lines, positions);
            }

            [Fact]
            public void Will_get_line_numbers_for_exports_javascript_tests()
            {
                var lines = new[] 
                {
                    "//JavaScript file",
                    "module.exports = {",
                    "  'test1':function(){expect(true).to.be.ok;},",
                    "  'module1': {",
                    "    'test2': function() { expect(false).to.not.be.ok; },",
                    "    'test3' : function () { expect(true).to.be.ok; },",
                    "    'module2': {",
                    "      'test4': function() { expect(true).to.be.ok;},",
                    "      'module3': {",
                    "        'test6': function ( ) { ",
                    "                   expect(true).to.be.ok;" +
                    "                 }",
                    "      }",
                    "    }",
                    "  }",
                    "}"
                };

                var positions = new[]
                {
                    new FilePosition(3, 4, "someTest1"),
                    new FilePosition(5, 6, "someTest2"),
                    new FilePosition(6, 6, "someTest3"),
                    new FilePosition(8, 8, "someTest4"),
                    new FilePosition(10, 10, "someTest5")
                };

                TestPositions(false, lines, positions);
            }

            [Fact]
            public void Will_get_line_numbers_for_bdd_coffeescript_tests()
            {
                var lines = new[] 
                {
                    "##CoffeeScript file",
                    "describe 'module1', ->",
                    "  it 'test1', -> expect(true).to.be.ok",
                    "  it 'test2', -> expect(false).to.not.be.ok"
                };

                var positions = new[]
                {
                    new FilePosition(3, 7, "someTest1"),
                    new FilePosition(4, 7, "someTest2")
                };

                TestPositions(true, lines, positions);
            }

            [Fact]
            public void Will_get_line_numbers_for_tdd_coffeescript_tests()
            {
                var lines = new[] 
                {
                    "##CoffeeScript file",
                    "suite 'module1', ->",
                    "  test 'test1', -> expect(true).to.be.ok",
                    "  test 'test2', -> expect(false).to.not.be.ok"
                };

                var positions = new[]
                {
                    new FilePosition(3, 9, "someTest1"),
                    new FilePosition(4, 9, "someTest2")
                };

                TestPositions(true, lines, positions);
            }

            [Fact]
            public void Will_get_line_numbers_for_qunit_coffeescript_tests()
            {
                var lines = new[] 
                {
                    "##CoffeeScript file",
                    "suite 'module1'",
                    "test 'test1', -> expect(true).to.be.ok",
                    "test 'test2', -> expect(false).to.not.be.ok"
                };

                var positions = new[]
                {
                    new FilePosition(3, 7, "someTest1"),
                    new FilePosition(4, 7, "someTest2"),
                };

                TestPositions(true, lines, positions);
            }
            
            [Fact]
            public void Will_get_line_numbers_for_exports_coffeescript_tests()
            {
                var lines = new[] 
                {
                    "##CoffeeScript file",
                    "module.exports = {",
                    "  'test1':->expect(true).to.be.ok,",
                    "  'module1': {",
                    "    'test2': -> expect(false).to.not.be.ok,",
                    "    'test3':()-> expect(true).to.be.ok,",
                    "    'module2': {",
                    "      'test4': () -> expect(true).to.be.ok,",
                    "      'module3': {",
                    "        'test6': ( ) ->" +
                    "             expect(true).to.be.ok",
                    "      }",
                    "    }",
                    "  }",
                    "}"
                };

                var positions = new[]
                {
                    new FilePosition(3, 4, "someTest1"),
                    new FilePosition(5, 6, "someTest2"),
                    new FilePosition(6, 6, "someTest3"),
                    new FilePosition(8, 8, "someTest4"),
                    new FilePosition(10, 10, "someTest5")
                };

                TestPositions(true, lines, positions);
            }

            [Fact]
            public void Will_get_line_number_for_test_with_quotes_in_title()
            {
                var processor = new TestableMochaLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, Path = "path" };
                processor.Mock<IFileSystemWrapper>().Setup(x => x.GetLines("path")).Returns(new string[] 
                {
                    "describe ( 'modu\"le\\'1', function () {",
                    " it (\'t\"e\\'st1\', function(){}); ",
                    "};"
                });

                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, "", new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal(2, file.FilePositions[0].Line);
                Assert.Equal(7, file.FilePositions[0].Column);
            }

            private static void TestPositions(bool coffeeScript, string[] lines, FilePosition[] positions)
            {
                var path = coffeeScript ? "path.coffee" : "path";
                var processor = new TestableMochaLineNumberProcessor();
                processor.Mock<IFileSystemWrapper>().Setup(x => x.GetLines(It.IsAny<string>())).Returns(lines);

                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, Path = path };

                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, String.Join("\n", lines), new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.True(
                    file.FilePositions.Contains(positions.Length - 1),
                    "Should not have less than the expected number of FilePositions");

                Assert.False(
                    file.FilePositions.Contains(positions.Length),
                    "Should not have more than the expected number of FilePositions");

                for (int i = 0; i < positions.Length; i++)
                {
                    var expectedPosition = positions[i];
                    var actualPosition = file.FilePositions[i];

                    Assert.True(
                        expectedPosition.Line == actualPosition.Line, 
                        string.Format("Line indexes should match for position {0}.\nActual: {1}, Expected: {2}", i, actualPosition.Line, expectedPosition.Line));

                    Assert.True(
                        expectedPosition.Column == actualPosition.Column, 
                        string.Format("Column indexes should match for position {0}.\nActual: {1}, Expected: {2}", i, actualPosition.Column, expectedPosition.Column));
                }
            }
        }
    }
}