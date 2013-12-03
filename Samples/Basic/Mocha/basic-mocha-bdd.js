/// <reference path="mocha.js" />
/// <reference path="chai.js" />
/// <reference path="../code.js" />

var expect = chai.expect;

it("A basic test", function() {
    expect(true).to.be.ok;
    var value = "hello";
    expect(value).to.equal("hello");
});

describe("stringLib", function() {
    it("will get vowel count", function() {
        var count = stringLib.vowels("hello");
        expect(count).to.equal(2);
    });
});

describe("mathLib", function() {
    it("will add 5 to number", function () {
        var res = mathLib.add5(10);
        expect(res).to.equal(15);
    });

    it("will multiply 5 to number", function () {
        var res = mathLib.mult5(10);
        expect(res).to.equal(55);
    });
});