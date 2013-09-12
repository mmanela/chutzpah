/// <reference path="chutzpahRunner.js" />
/// <reference path="mocha.js" />
/*globals phantom, chutzpah, window, mocha*/

(function() {
    'use strict';
    phantom.injectJs('chutzpahRunner.js');

    var startTime = null;
    var activeTestCase = null;
    
    chutzpah.isTestingFinished = false;
    chutzpah.testCases = [];
    
    var passed = 0;
    var failed = 0;
    var skipped = 0;

    var mocha;

    function log(obj) {
        console.log(JSON.stringify(obj));
    }
    
    function logCoverage() {
        if (window._Chutzpah_covobj_name && window[window._Chutzpah_covobj_name]) {
            log({ type: "CoverageObject", object: window[window._Chutzpah_covobj_name] });
        }
    }
    
    var chutzpahMochaReporter = function(runner) {
        runner.on('start', function() {
            startTime = new Date();
            
            log({ type: "FileStart" });
        });
        
        runner.on('end', function() {
            logCoverage();

            log({
                type: "FileDone", 
                timetaken: new Date() - startTime, 
                passed: passed, 
                failed: failed
            });           

            chutzpah.isTestingFinished = true;
        });
        
        runner.on('suite', function(suite) {
            chutzpah.currentModule = suite.fullTitle();
        });
        
        runner.on('suite end', function(suite) {
            chutzpah.currentModule = null;
        });
        
        runner.on('test', function(test) {
             activeTestCase = {
                 moduleName: chutzpah.currentModule, 
                 testName: test.title, 
                 testResults: []
             };
             chutzpah.testCases.push(activeTestCase);
             log({ type: "TestStart", testCase: activeTestCase });
        });
        
        runner.on('test end', function(test) {
            activeTestCase.timetaken = test.duration;

            log({ type: "TestDone", testCase: activeTestCase });
        });
        
        //runner.on('hook', function(hook) { });
        //runner.on('hook end', function(hook) { });
        
        runner.on('pass', function(test) {
             passed++;
        });
        
        runner.on('fail', function(test, err) {
             failed++;
        });
        
        runner.on('pending', function(test) {
             skipped++;
        });
    };

    function onInitialized() {
        console.log("!!_!! onInitialized");

        var oldOnLoad = window.onload;

        window.onload = function () {
            if (oldOnLoad) {
                oldOnLoad();
            }

            log("!!_!! Starting Mocha...");

            var Mocha = require('mocha');
            mocha = new Mocha({ reporter: chutzpahMochaReporter });
        };
    }

    function onPageLoaded() {
        console.log("!!_!! onPageLoaded");
    }

    function isMochaLoaded() {
        console.log("!!_!! isMochaLoaded");
        
        return mocha != null;
    }

    function onMochaLoaded() {
        console.log("!!_!! onMochaLoaded");

        chutzpah.isTestingFinished = false;
        chutzpah.testCases = [];

        if (chutzpah.testMode === 'discovery') {
            
        }
        
        mocha.run();
    }

    function isTestingDone() {
        return chutzpah.isTestingFinished === true;
    }

    try {
        chutzpah.runner(onInitialized, onPageLoaded, isMochaLoaded, onMochaLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
