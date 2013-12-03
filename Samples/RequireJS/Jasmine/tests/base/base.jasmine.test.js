define(['base/core', 'hello'],
    function (core) {
        describe("base/core", function () {
            it("will return correct version from core", function () {
                var version = core.version;
                expect(version).toEqual(8);
                
                var body = $('body');
                var greeting = body.hello();
                expect(greeting).toEqual('Hi there!!!');
            });
        });
    });
