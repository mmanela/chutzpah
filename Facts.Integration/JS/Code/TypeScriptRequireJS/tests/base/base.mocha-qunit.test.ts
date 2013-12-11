/// <chutzpah_reference path="mocha.js" />
/// <reference path="../../mocha.d.ts" />
/// <reference path="../../chai.d.ts" />
/// <reference path="../../mocha-qunit.d.ts" />


var expect = chai.expect;

import core = require('base/core');

suite("base/core");
test("will return correct version from core", function () {
    var version = core.version;
    expect(version).to.equal(8);
});
