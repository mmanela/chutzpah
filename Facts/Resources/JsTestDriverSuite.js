/// <reference path="browser_controlled_runner.js" />
// Example copied from http://code.google.com/p/js-test-driver/source/browse/samples/hello-world/src-test/GreeterTest.js

GreeterTest = TestCase("GreeterTest");

GreeterTest.prototype.testGreet = function () {
    var greeter = new myapp.Greeter();
    assertEquals("Hello World!", greeter.greet("World"));
    jstestdriver.console.log("JsTestDriver", greeter.greet("World"));
    console.log(greeter.greet("Browser", "World"));
};
