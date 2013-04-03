/// <reference path="../../../code/code.js" />

  test("Will have no files when include and exclude settings remove all", function () {
      var count = stringLib.vowels("hello");
      equal(count, 2);
  });