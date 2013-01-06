/// <reference path="../../../Code/RequireJS/require.js" />

requirejs(['base/core', 'ui/screen'],
    function (core, screen) {

        module("base/core");
        test("will return correct version from core", function () {
            var version = core.version;
            equal(version, 8);
        });

        module("ui/screen");
        test("will build display version", function () {
            var disp = screen.displayVersion;
            equal(disp, "Version: 8");
        });


    });
