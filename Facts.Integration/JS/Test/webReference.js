/// <reference path="http://code.jquery.com/jquery.min.js" />

$(function(){

    test("A jquery test", function () {
      ok($.inArray(2,[1,2,3]), "this test is fine");
  });
});