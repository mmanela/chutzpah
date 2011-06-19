/// <reference path="http://code.jquery.com/jquery-1.5.2.min.js" />

$(function(){

    test("A jquery test", function () {
      ok($.inArray(2,[1,2,3]), "this test is fine");
  });
});