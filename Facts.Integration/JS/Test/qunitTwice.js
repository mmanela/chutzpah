// This will cause Chutzpah to load qunit twice
/// <reference path="http://code.jquery.com/qunit/qunit-1.10.0.js" />

 test("A basic test", function () {
      ok(true, "this test is fine");
      var value = "hello";
      equal("hello", value, "We expect value to be hello");
  });