// THIS DO NOT WORK YET. 

/// <reference path="mocha.js" />
/// <reference path="chai.js" />
/// <reference path="../code/code-coffee.coffee" />

var expect = chai.expect;
exports.basic = {
    "A basic test": function() {
        expect(true).to.be.ok;
        var value = "hello";
        expect(value).to.equal("hello");
    },
    "stringLib": {
        "will get vowel count": function() {
            var count = stringLib.vowels("hello");
            expect(count).to.equal(2);
        }
    },
    "mathLib": {
        "will add 5 to number": function() {
            var res = mathLib.add5(10);
            expect(res).to.equal(15);
        },
        "will multiply 5 to number": function() {
            var res = mathLib.mult5(10);
            expect(res).to.equal(55);
        }
    }
};