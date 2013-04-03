/// <reference path="../../../code/code.js" />

  test("Will have files when include and exclude settings are given", function () {
      var count = stringLib.vowels("hello");
      equal(count, 2);
  });