/// <reference path="require.js" />
/// <reference path="jasmine.js" />

window.chutzpah.preventAutoStart();

requirejs(['./tests/base/base.jasmine.test',
           './tests/ui/ui.jasmine.test'],
    function () {

        window.chutzpah.start();
    }
);
