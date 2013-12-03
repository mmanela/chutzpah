/// <chutzpah_reference path="mocha.js" />
/// <chutzpah_reference path="chai.js" />
/// <reference path="mocha.d.ts" />
/// <reference path="chai.d.ts" />
/// <reference path="../code.ts" />

var expect = chai.expect;

it("A basic test", function() {
    expect(true).to.be.ok;
    var value = "hello";
    expect(value).to.equal("hello");
});

describe("stringLib", function() {
    it("will get vowel count", function() {
        var stringPlus = new StringPlus("hello");
        
        var count = stringPlus.countVowels();
        
        expect(count).to.equal(2);
    });
});

describe("mathLib", function() {
    it("will add 5 to number", function () {
        var res:number = mathLib.add5(10);
        expect(res).to.equal(15);
    });

    it("will multiply 5 to number", function () {
        var res = mathLib.mult5(10);
        expect(res).to.equal(55);
    });
});