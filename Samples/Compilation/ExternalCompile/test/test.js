/// <reference path="qunit.d.ts" />
/// <reference path="../src/code.ts" />
QUnit.module("stringLib");

test("will get vowel count", function () {
    var stringPlus = new StringPlus("hello");

    var count = stringPlus.countVowels();

    equal(count, 2, "We expect 2 vowels in hello");
});

QUnit.module("mathLib");

test("will add 5 to number", function () {
    var res = mathLib.add5(10);

    equal(res, 15, "should add 5");
});
//# sourceMappingURL=test.js.map
