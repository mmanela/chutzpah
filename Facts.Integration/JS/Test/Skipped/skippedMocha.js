/// <reference path="mocha.js" />
/// <reference path="../chai.js" />

var expect = chai.expect;

it("Passed", function () {
    expect(true).to.be.ok;
});

it.skip("Skipped", function () {
    expect(false).to.be.ok;
});

it("Failed", function () {
    expect(false).to.be.ok;
});
