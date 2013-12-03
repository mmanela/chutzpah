import screen = require('ui/screen');

QUnit.module("ui/screen");
test("will build display version", function () {
    var disp = screen.displayVersion;
    equal(disp, "Version: 8");
});

