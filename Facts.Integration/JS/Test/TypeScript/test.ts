/// <reference path="child.ts" />

test("inheritance working?", function () {
        var message = "a message";
        var myc = new MyClass(message);
        equal(myc.message, message, "message should get passed through");
});