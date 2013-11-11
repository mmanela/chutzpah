/// <reference path="mocha.js"/>

var expect = chai.expect;

define(['base/core'],
    function (core) {

        suite("base/core");
        test("will return correct version from core", function () {
            var version = core.version;
            expect(version, "We expect value to be 8").to.equal(8);
        });

    });
