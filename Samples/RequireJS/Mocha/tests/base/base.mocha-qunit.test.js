var expect = chai.expect;

define(['base/core', 'hello'],
    function (core) {

        suite("base/core");
        test("will return correct version from core", function () {
            var version = core.version;
            expect(version, "We expect value to be 8").to.equal(8);
            
            var body = $('body');
            var greeting = body.hello();
            expect(greeting, "We expect value to be a nice greeting").to.equal('Hi there!!!');
        });

    });
