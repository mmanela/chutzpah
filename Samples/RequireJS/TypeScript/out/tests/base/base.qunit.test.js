define(["require", "exports", "base/core"], function (require, exports, core) {
    "use strict";
    exports.__esModule = true;
    QUnit.module("base/core");
    test("will return correct version from core", function () {
        var version = core.version;
        equal(version, 8);
    });
});
//# sourceMappingURL=base.qunit.test.js.map