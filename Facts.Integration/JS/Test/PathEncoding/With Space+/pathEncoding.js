/// <reference path="../../../code/code.js" />

  test("Will encode file path", function () {
      var count = stringLib.vowels("hello");

      equal(count, 2, "We expect 2 vowels in hello");
  });
