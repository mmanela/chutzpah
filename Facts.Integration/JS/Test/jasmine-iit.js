
describe("ignored suite", function () {
    it("ignored spec", function () {
        expect(true).toBe(false);
    });
});

describe("executed suite", function () {
    iit("exclusive spec", function () {
        expect(true).toBe(true);
    });
});
