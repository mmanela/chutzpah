/// <reference path="../../../code/code.js" />


  test("verify from custom harness", function() {
      ok(window.isCustomHarness);
  });
  
  module("stringLib");

  test("will get vowel count", function () {
      var count = stringLib.vowels("hello");

      equal(count, 2, "We expect 2 vowels in hello");
  });
