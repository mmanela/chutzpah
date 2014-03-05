define(['base/core', 'hello'],
    function (core) {
        module("base/core");
        test("will return correct version from core", function () {
            var version = core.version;
            equal(version, 8);
            
            var body = $('body');
            var greeting = body.hello();
            equal(greeting, 'Hi there!!!');
        });
    });

