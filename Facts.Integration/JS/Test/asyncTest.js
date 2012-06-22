/// <reference path="../code/code.js" />

asyncTest("A basic test", function () {
    ok(true, "this test is fine");
    var value = "hello";
    equal("hello", value, "We expect value to be hello");
    start();
});

asyncTest("will get vowel count", function () {
    var count = stringLib.vowels("hello");

    equal(count, 2, "We expect 2 vowels in hello");
    start();
});