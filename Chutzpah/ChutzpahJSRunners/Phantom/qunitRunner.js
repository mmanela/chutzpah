/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, QUnit*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');
    phantom.injectJs('../qunitFunctions.js');
    
    try {
        chutzpah.runner(onInitialized, onPageLoaded, isQunitLoaded, onQUnitLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
} ());
