define(["require", "exports", "ui/screen"], function (require, exports, screen) {
    "use strict";
    exports.__esModule = true;
    QUnit.module("ui/screen");
    test("will build display version", function () {
        var disp = screen.displayVersion;
        equal(disp, "Version: 8");
    });
});
//# sourceMappingURL=ui.qunit.test.js.map