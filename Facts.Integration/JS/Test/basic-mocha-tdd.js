/// <reference path="mocha.js" />
/// <reference path="chai.js" />
/// <reference path="../code/code.js" />

var assert = chai.assert;

test("A basic test", function () {
    assert.ok(true);
    var value = "hello";
    assert.equal(value, "hello");
});

suite("stringLib", function() {
    test("will get vowel count", function () {
        var count = stringLib.vowels("hello");
        assert.equal(count, 2);
    });
});

suite("mathLib", function() {
    test("will add 5 to number", function () {
        var res = mathLib.add5(10);
        assert.equal(res, 15);
    });

    test("will multiply 5 to number", function () {
        var res = mathLib.mult5(10);
        assert.equal(res, 55);
    });
});