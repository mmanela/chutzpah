/// <reference path="../../../code/code.js" />

describe("general", function () {
    it("A basic test", function () {
        expect(true).toBeTruthy();
        var value = "hello";
        expect("hello").toEqual(value);
    });
});
