describe("some suite", function() {
    it("should have referenced chai", function() {
        chai.expect(chai).to.exist;
    });
    it("should not have referenced someLib", function () {
        chai.expect(typeof someLib).to.equal("undefined");
    });
});
