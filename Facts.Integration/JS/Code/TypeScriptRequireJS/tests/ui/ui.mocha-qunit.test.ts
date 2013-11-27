/// <chutzpah_reference path="mocha.js" />
/// <reference path="../../mocha.d.ts" />
/// <reference path="../../chai.d.ts" />
/// <reference path="../../mocha-qunit.d.ts" />


var expect = chai.expect;

import screen = require('ui/screen');

suite("ui/screen");
test("will build display version", function () {
    var disp = screen.displayVersion;
    expect(disp).to.equal("Version: 8");
});

