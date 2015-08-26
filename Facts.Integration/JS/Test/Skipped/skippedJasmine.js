/// <reference path="jasmine.js" />

describe("Skipped Tests", function () {
    it("Passed", function () {
        expect(true).toBeTruthy();
    });

    it("Skipped", function () {
        pending();
        expect(false).toBeTruthy();
    });

    it("Failed", function () {
        expect(false).toBeTruthy();
    });
});