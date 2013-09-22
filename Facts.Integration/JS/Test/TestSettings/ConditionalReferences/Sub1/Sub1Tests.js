describe("some suite", function() {
    it("should have referenced chai", function() {
        chai.expect(chai).to.exist;
    });

    var expect = chai.expect;

    it("should have referenced someLib in settings relative folder", function () {
        expect(someLib).to.exist;
        expect(someLib.hello()).to.equal("Hello there");
    });

    it("should have referenced localFile.js in settings folder", function () {
        expect(globalValue).to.equal("expected value");
    });
});
