using System;
using System.Linq;
using Xunit.Extensions;

namespace Chutzpah.Facts.Library
{
    using Chutzpah.FileProcessors;
    using Chutzpah.Models;
    using Chutzpah.Wrappers;
    using Moq;
    using Xunit;

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

                processor.ClassUnderTest.Process(file);

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
                    new FilePosition(3, 3),
                    new FilePosition(4, 5)
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
                    new FilePosition(3, 3),
                    new FilePosition(4, 5)
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
                    new FilePosition(3, 3),
                    new FilePosition(4, 5)
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
                    new FilePosition(3, 3),
                    new FilePosition(5, 5),
                    new FilePosition(6, 5),
                    new FilePosition(8, 7),
                    new FilePosition(10, 9)
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
                    new FilePosition(3, 3),
                    new FilePosition(4, 3)
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
                    new FilePosition(3, 3),
                    new FilePosition(4, 3)
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
                    new FilePosition(3, 1),
                    new FilePosition(4, 1),
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
                    new FilePosition(3, 3),
                    new FilePosition(5, 5),
                    new FilePosition(6, 5),
                    new FilePosition(8, 7),
                    new FilePosition(10, 9)
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

                processor.ClassUnderTest.Process(file);

                Assert.Equal(2, file.FilePositions[0].Line);
                Assert.Equal(2, file.FilePositions[0].Column);
            }

            private static void TestPositions(bool coffeeScript, string[] lines, FilePosition[] positions)
            {
                var path = coffeeScript ? "path.coffee" : "path";
                var processor = new TestableMochaLineNumberProcessor();

                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, Path = path };

                processor.Mock<IFileSystemWrapper>().Setup(x => x.GetLines(path)).Returns(lines);

                processor.ClassUnderTest.Process(file);

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