/// <reference path="jasmine.js" />
/// <reference path="references.js" />

describe("References Testing", function () {
    it("Test importing from references file", function () {
        var calculator = new Calculator();
        var num = 10;

        var res1 = mathLib.mult5(num);
        var res2 = calculator.multiply(5, num);

        expect(res1).toEqual(res2);
        expect(window.rightOrder).toBeTruthy();
    });
});
