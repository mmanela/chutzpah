/// <reference path="require.js" />
/// <reference path="mocha.js" />

window.chutzpah.preventAutoStart();

requirejs(['../chai.js',
           './tests/base/base.mocha-qunit.test',
           './tests/ui/ui.mocha-qunit.test'],
    function (chai) {

        window.expect = chai.expect;
        window.chutzpah.start();
    }
);
