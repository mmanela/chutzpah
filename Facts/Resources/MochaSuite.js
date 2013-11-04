/// <reference path="mocha.js" />
// Example copied from https://github.com/michaelphines/Jasmine-Examples/blob/master/example-3/spec/javascript/exampleSpec.js

describe('Greeter', function () {
    describe('#sayHello()', function () {
        var greeter;
        beforeEach(function () {
            greeter = new Greeter('');
        });

        it('says hello', function () {
            expect(greeter.sayHello()).toMatch(/^Hello, .*!$/);
        });

        it('uses the name', function () {
            spyOn(greeter, 'getName').andReturn('name');
            expect(greeter.sayHello()).toMatch(/name/);
            expect(greeter.getName).wasCalled();
        });
    })

    describe('#name()', function () {
        it('gets the name', function () {
            var greeter = new Greeter('name');
            expect(greeter.getName()).toEqual('name');
        })
    });
});