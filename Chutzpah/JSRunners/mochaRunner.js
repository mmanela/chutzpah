/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, mocha, Mocha */

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function onInitialized() { }

    function isTestingDone() {
        return window.chutzpah.isTestingFinished === true;
    }
    
    function isMochaLoaded() {
        return window.mocha;
    }
    
    function onMochaLoaded() {
        function log(obj) {
            console.log(JSON.stringify(obj));
        }

        var activeTestCase = null,
            fileStartTime = null,
            testStartTime = null;
        window.chutzpah.isTestingFinished = false;
        window.chutzpah.testCases = [];

        function logCoverage() {
            if (window._Chutzpah_covobj_name && window[window._Chutzpah_covobj_name]) {
                log({ type: "CoverageObject", object: window[window._Chutzpah_covobj_name] });
            }
        }

        var ChutzpahMochaReporter = function (runner) {

            window.Mocha.reporters.Base.call(this, runner);

            // Base manages a stats object like: { suites: 0, tests: 0, passes: 0, pending: 0, failures: 0 }
            
            var self = this, stats = this.stats, failures = this.failures;

            if (!runner) return;
            this.runner = runner;

            runner.stats = stats;

            runner.on('start', function () {
                
                // Testing began
                log({ type: "FileStart" });
            });

            runner.on('end', function () {
                logCoverage();
                log({ type: "FileDone", timetaken: stats.duration, passed: stats.passes, failed: stats.failures });
                window.chutzpah.isTestingFinished = true;
            });
            
            
            runner.on('test', function(test) {
                var suiteName = test.parent ? test.parent.fullTitle() : null;
                var specName = test.title;
                var newTestCase = { moduleName: suiteName, testName: specName, testResults: [] };
                window.chutzpah.testCases.push(newTestCase);
                activeTestCase = newTestCase;
                log({ type: "TestStart", testCase: activeTestCase });
            });
            
            runner.on('test end', function (test) {
                activeTestCase.timetaken = test.duration;
  
                var testResult = {};
                testResult.passed = test.state == 'passed';
                
                if (!testResult.passed && !test.pending) {
                    var str = test.err.stack || test.err.toString();
                    testResult.message = test.err.message;
                    testResult.stackTrace = str;

                    console.log("MESSAGE: " + test.err.message);
                    console.log("STACK: " + test.err.stack);
                    console.log("ERRTS: " + test.err.toString());
                    console.log("ACTUAL: " + testResult.actual);
                    console.log("EXPECTED: " + testResult.expected);
                    
                    if (test.err.actual !== undefined || test.err.expected !== undefined) {
                        testResult.actual = test.err.actual;
                        testResult.expected = test.err.expected;
                    }
                }
                
                activeTestCase.testResults.push(testResult);
      
                // Log test case when done. This will get picked up by phantom and streamed to chutzpah.
                log({ type: "TestDone", testCase: activeTestCase });
            });
        };

        if (window.chutzpah.testMode) {
            window.mocha.reporter(ChutzpahMochaReporter);
        }

        if (window.chutzpah.testMode === 'discovery') {
            // TODO: Implement discoverery optimization
        }
    }

    function onPageLoaded() {
        var _cachedWindowLoad = window.onload;
        window.onload = function () {
            if (_cachedWindowLoad) {
                _cachedWindowLoad();
            }

            //window.mocha.run();
        };
    }

    try {
        chutzpah.runner(onInitialized, onPageLoaded, isMochaLoaded, onMochaLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unknown error
    }
}());
