/// <reference path="chutzpah.js" />
/*globals phantom, chutzpah, window*/

(function () {
    'use strict';
    
    phantom.injectJs('chutzpahRunner.js');

    function testsComplete() {
        return window.chutzpah.isRunning === false;
    }

    try {
        chutzpah.runner(testsComplete);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
} ());
