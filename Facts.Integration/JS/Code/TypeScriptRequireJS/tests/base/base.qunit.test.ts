/// <reference path="../../qunit.d.ts" />

define(['base/core'],
    function (core) {

        QUnit.module("base/core");
        test("will return correct version from core", function () {
            var version = core.version;
            equal(version, 8);
        });

    });
