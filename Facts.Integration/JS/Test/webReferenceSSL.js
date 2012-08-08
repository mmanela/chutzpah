/// <reference path="https://ajax.googleapis.com/ajax/libs/jquery/1.7.2/jquery.min.js" />

$(function(){

    test("A jquery test", function () {
      ok($.inArray(2,[1,2,3]), "this test is fine");
  });
});