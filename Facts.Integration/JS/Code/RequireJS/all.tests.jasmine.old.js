/// <reference path="require-2.0.2.js" />
/// <reference path="jasmine.js" />

require.config({

    paths: {
        alpha: 'base/jquery.alpha',
    },

    shim: {
        alpha: { deps: ["jquery"] },
    }
});

requirejs(['./tests/base/base.jasmine.test',
           './tests/ui/ui.jasmine.test'],
    function () { }
);
