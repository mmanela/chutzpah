console.log("!!! IN DEFINE");
define(['base/core', 'alpha'],
    function (core) {
        module("base/core");
        test("will return correct version from core", function () {
            console.log("!!! IN TEST");
            var version = core.version;
            equal(version, 8);
            
            var body = $('body');
            body.alpha();
            ok(body, 'good');
        });
    });
