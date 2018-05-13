/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, mocha*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');
    phantom.injectJs('../mochaFunctions.js');
    
    try {
        chutzpah.runner(onInitialized, onPageLoaded, isMochaLoaded, onMochaLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
