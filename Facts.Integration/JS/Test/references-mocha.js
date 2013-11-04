/// <reference path="references.js" />
/// <reference path="mocha.js" />
/// <reference path="chai.js" />

var expect = chai.expect;

 describe("References Testing", function() {
     it("Test importing from references file", function() {
         var calculator = new Calculator();
         var num = 10;

         var res1 = mathLib.mult5(num);
         var res2 = calculator.multiply(5, num);

         expect(res1).to.equal(res2);
         expect(window.rightOrder).to.be.ok;
     });
 });