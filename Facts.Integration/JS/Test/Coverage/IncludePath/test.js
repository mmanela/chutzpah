/// <reference path="../../../code/code.js" />

  test("Will include paths from chutzpah settings file", function () {
      var count = stringLib.vowels("hello");
      equal(count, 2);
  });