/// <reference path="references.js" />


 module("References Testing");
 test("Test importing from references file", function () {
     var calculator = new Calculator();
     var num = 10;

     var res1 = mathLib.mult5(num);
     var res2 = calculator.multiply(5, num);

     equal(res1, res2);
 });