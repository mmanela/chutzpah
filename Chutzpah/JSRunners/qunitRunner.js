/// <reference path="chutzpah.js" />
/*globals phantom, chutzpah, window*/

(function () {
    'use strict';
    
    phantom.injectJs('chutzpahRunner.js');

    function testsComplete() {

        // If in discovery mode we know all tests at load time
        if (window.chutzpah.testMode === 'discovery') {
            return true;
        }
        
        return !window.chutzpah.isRunning;
    }


    try {
        chutzpah.runner(testsComplete);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
} ());
