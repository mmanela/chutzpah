/// <reference path="jquery-1.7.1.min.js" />
/// <reference path="qunit.js" />
/// <reference path="pavlov.js" />
/// <reference path="../code/Calculator.js" />

pavlov.specify("The behavior of a calculator", function () {
    describe("Calculator", function () {
        var calculator;
        before(function () {
            calculator = new Calculator();
        });

        given([2, 0], [3, 0], [4, 0]).
            it("returns zero when multiplying by zero", function (x, y) {
                assert(0).equal(calculator.multiply(x, y));
            });

        given([2, 1], [3, 1], [4, 1]).
            it("returns the multiplicand when multiplying by one", function (x, y) {
                assert(x).equal(calculator.multiply(x, y));
            });
    });
});