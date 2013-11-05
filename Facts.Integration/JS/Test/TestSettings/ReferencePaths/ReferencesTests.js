describe("some suite", function() {
    it("should have referenced chai", function() {
        chai.expect(chai).to.exist;
    });
    it("Will include files from folder", function () {
        chai.expect(takeThis).to.equal(true);
        chai.expect(typeof skipThis).to.equal("undefined");
    });
});
