/// <reference path="../code.js" />
/// <reference path="jasmine.js" />

describe("general", function () {
    it("A basic test", function () {
        expect(true).toBeTruthy();
        var value = "hello";
        expect("hello").toEqual(value);
    });
});

describe("stringLib", function () {
    it("will get vowel count", function () {
        var count = stringLib.vowels("hello");
        expect(count).toEqual(2);
    });
});

describe("mathLib", function () {
    beforeEach(function () {
        console.log("beforeEach");
    });
    it("will add 5 to number", function () {
        var res = mathLib.add5(10);
        expect(res).toEqual(15);
    });

    it("will multiply 5 to number", function () {
        var res = mathLib.mult5(10);
        expect(res).toEqual(55);
    });
});
