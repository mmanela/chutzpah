// This test validates that Chutzpah detects the Chutzpah.json file and uses its Framework settings to determine the test framework
var run = test;

run("A basic test", function () {
    ok(true, "this test is fine");
    var value = "hello";
    equal("hello", value, "We expect value to be hello");
});