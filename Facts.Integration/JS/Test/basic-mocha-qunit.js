/// <reference path="../code/code.js" />
/// <reference path="mocha.js" />
/// <reference path="chai.js" />
var expect = chai.expect;

test("A basic test", function () {
    expect(true).to.be.ok;
    var value = "hello";
    expect(value, "We expect value to be hello").to.equal("hello");
});

suite("stringLib");

test("will get vowel count", function () {
    var count = stringLib.vowels("hello");

    expect(count, "We expect 2 vowels in hello").to.equal(2);
});

suite("mathLib");

test("will add 5 to number", function () {
    var res = mathLib.add5(10);

    expect(res, "should add 5").to.equal(15);
});

test("will multiply 5 to number", function () {
    var res = mathLib.mult5(10);

    expect(res, "should multiply by 5").to.equal(55);
});
