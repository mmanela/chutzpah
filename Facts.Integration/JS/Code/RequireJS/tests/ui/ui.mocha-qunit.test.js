/// <reference path="../../require.js" />

define(['ui/screen'],
    function (screen) {
        module("ui/screen");
        test("will build display version", function () {
            var disp = screen.displayVersion;
            expect(disp, "We expect value to be Version: 8").to.equal("Version: 8");
        });

    });
