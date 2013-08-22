define(['base/core', 'alpha'],
    function (core) {
        describe("base/core", function () {
            it("will return correct version from core", function () {
                var version = core.version;
                expect(version).toEqual(8);
                
                var body = $('body');
                body.alpha();
                expect(body).toBeTruthy();
            });
        });
    });
