/// <reference path="jquery-1.7.1.min.js" />
/// <reference path="qunit.js" />
/// <reference path="../code/Calculator.js" />

var calculator;

module("Calculator", {
    setup: function () {
        calculator = new Calculator();
    }
});

$.each([[2, 0], [3, 0], [4, 0]], function (index, pair) {
    test("given " + pair[0] + "," + pair[1] + ", returns zero when multiplying by zero", function () {
        equal(0, calculator.multiply(pair[0], pair[1]));
    });
});

$.each([[2, 1], [3, 1], [4, 1]], function (index, pair) {
    test("given " + pair[0] + "," + pair[1] + ", returns the multiplicand when multiplying by one", function () {
        equal(pair[0], calculator.multiply(pair[0], pair[1]));
    });
});