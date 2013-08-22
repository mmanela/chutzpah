/// <reference path="require-2.0.2.js" />
/// <reference path="qunit.js" />

require.config({
    
    paths: {
        alpha: 'base/jquery.alpha',
    },

    shim: {
        alpha: { deps: ["jquery"] },
    }
});

requirejs(['./tests/base/base.qunit.test',
           './tests/ui/ui.qunit.test'],
    function (){}
);