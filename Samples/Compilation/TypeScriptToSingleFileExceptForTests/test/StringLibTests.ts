/// <reference path="qunit.d.ts" />
/// <reference path="../_out/merged.d.ts" />

QUnit.module("stringLib");

test("will get vowel count", function () {
    var stringPlus = new StringPlus("hello");

    var count = stringPlus.countVowels();

    equal(count, 2, "We expect 2 vowels in hello");
});
