/// <reference path="require.js" />
/// <reference path="mocha.js" />
/// <reference path="chai.js" />

var expect = chai.expect;

requirejs(['./tests/base/base.mocha-qunit.test',
           './tests/ui/ui.mocha-qunit.test'],
    function (){}
);
