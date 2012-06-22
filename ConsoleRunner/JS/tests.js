/// <reference path="../js/qunit.js" />
/// <reference path="../js/code.js" />
///  <reference path="http://code.jquery.com/jquery-1.5.2.min.js" />
//  <reference path="jquery-1.5.2.min.js" />


$(function () {

    test("A basic test", function () {
        ok(true, "this test is fine");
        var value = "hello";
        equal("hello", value, "We expect value to be hello");
    });

    module("stringLib");

    test("will get vowel count", function () {
        var count = stringLib.vowels("hello");

        equal(count, 2, "We expect 2 vowels in hello");
    });

    module("mathLib");

    test("will add 5 to number", function () {
        var res = mathLib.add5(10)

        equal(res, 15, "should add 5");
    });

    test("will multiply 5 to number", function () {
        var res = mathLib.mult5(10)

        equal(res, 55, "should multiply by 5");
    });

    test('testing "quotes" to \'quotes\' for <title> text', function () {
        equal(" this is a quote\" <-- see", "this is not ' <script>hello</script>");
        equal(10, 11);
    });


    test("syntax", function () {
        var a = 2;
        adds;
    });
});