/// <reference path="mocha.js"/>

var expect = chai.expect;

define(['ui/screen'],
    function (screen) {
        suite("ui/screen");
        test("will build display version", function () {
            var disp = screen.displayVersion;
            expect(disp, "We expect value to be Version: 8").to.equal("Version: 8");
        });

    });
