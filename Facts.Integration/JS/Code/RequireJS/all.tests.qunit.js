/// <reference path="require.js" />
/// <reference path="qunit.js" />


window.chutzpah.preventAutoStart();

requirejs(['./tests/base/base.qunit.test',
           './tests/ui/ui.qunit.test'],
    function () {
        window.chutzpah.start();
    }
);
