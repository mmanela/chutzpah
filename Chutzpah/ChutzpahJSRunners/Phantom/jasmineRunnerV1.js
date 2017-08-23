/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, jasmine*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');
    phantom.injectJs('../jasmineFunctionsV1.js');

    try {
        chutzpah.runner(onInitialized, onPageLoaded, isJasmineLoaded, onJasmineLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
