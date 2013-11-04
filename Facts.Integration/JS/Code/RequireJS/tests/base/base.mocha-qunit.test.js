define(['base/core'],
    function (core) {

        module("base/core");
        test("will return correct version from core", function () {
            var version = core.version;
            expect(version, "We expect value to be 8").to.equal(8);
        });

    });
