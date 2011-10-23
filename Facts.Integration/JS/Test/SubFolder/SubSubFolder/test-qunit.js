/// <reference path="../../../code/code.js" />

 test("A basic test", function () {
      ok(true, "this test is fine");
      var value = "hello";
      equals("hello", value, "We expect value to be hello");
  });