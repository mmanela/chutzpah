/// <reference path="../../../Code/RequireJS/require.js" />

requirejs(['base/core', 'ui/screen'],
    function (core, screen) {

        describe("base/core", function () {
            it("will return correct version from core", function () {
                var version = core.version;
                expect(version).toEqual(8);
            });
        });

        describe("ui/screen", function () {
            it("will build display version", function () {
                var disp = screen.displayVersion;
                expect(disp).toEqual("Version: 8");
            });
        });
    });
