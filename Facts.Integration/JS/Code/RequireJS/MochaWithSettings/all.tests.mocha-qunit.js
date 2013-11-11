/// <reference path="../require.js" />
/// <reference path="mocha.js" />


requirejs(['./tests/base/base.mocha-qunit.test',
           './tests/ui/ui.mocha-qunit.test'],
    function () {

        window.expect = chai.expect;
    }
);
