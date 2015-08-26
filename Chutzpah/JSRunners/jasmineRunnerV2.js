/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, jasmine*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function onInitialized() {
        console.log("!!_!! onInitialized  Jasmine - v2");
    }

    function isTestingDone() {
        console.log("!!_!! isTestingDone");
        return window.chutzpah.isTestingFinished === true;
    }

    function isJamineLoaded() {
        console.log("!!_!! isJasmineLoaded");
        return window.jasmine;
    }

    function onJasmineLoaded() {

        console.log("!!_!! onJasmineLoaded");

        function log(obj) {
            console.log(JSON.stringify(obj));
        }


        var activeTestCase = null,
            fileStartTime = null,
            testStartTime = null,
            suites = [];

        window.chutzpah.isTestingFinished = false;
        window.chutzpah.testCases = [];

        function logCoverage() {
            if (window._Chutzpah_covobj_name && window[window._Chutzpah_covobj_name]) {
                log({ type: "CoverageObject", object: JSON.stringify(window[window._Chutzpah_covobj_name]) });
            }
        }

        function recordStackTrace(stack) {
            if (stack) {
                // Truncate stack to 5 deep. 
                stack = stack.split('\n').slice(1,6).join('\n');
            }
            return stack;
        }


        function ChutzpahJasmineReporter(options) {

            var passedCount = 0;
            var failedCount = 0;
            var skippedCount = 0;

            this.jasmineStarted = function () {

                fileStartTime = new Date().getTime();

                // Testing began
                log({ type: "FileStart" });
            };

            
            this.jasmineDone = function () {
                var timetaken = new Date().getTime() - fileStartTime;
                logCoverage();
                log({ type: "FileDone", timetaken: timetaken, passed: passedCount, failed: failedCount });
                window.chutzpah.isTestingFinished = true;
            };


            this.suiteStarted = function (result) {
                suites.push(result);
            };

            this.suiteDone = function (result) {
                suites.pop();
            };

            this.specStarted = function (result) {
                var currentSuiteName = suites.length > 0
                                        ? suites[suites.length - 1].fullName
                                        : null;

                testStartTime = new Date().getTime();
                var suiteName = currentSuiteName;
                var specName = result.description;
                var newTestCase = { moduleName: suiteName, testName: specName, testResults: [] };
                window.chutzpah.testCases.push(newTestCase);
                activeTestCase = newTestCase;
                log({ type: "TestStart", testCase: activeTestCase });
            };

            this.specDone = function (result) {
                if (result.status === "disabled") {
                    return;
                }

                if (result.status === "failed") {
                    failedCount++;
                }
                else if (result.status === "pending") {
                    skippedCount++;
                    activeTestCase.skipped = true;
                }
                else {
                    passedCount++;
                }

                var timetaken = new Date().getTime() - testStartTime;
                activeTestCase.timetaken = timetaken;

                for (var i = 0; i < result.failedExpectations.length; i++) {
                    var expectation = result.failedExpectations[i];

                    var testResult = {};
                    testResult.passed = false;
                    testResult.message = expectation.message;
                    testResult.stackTrace = recordStackTrace(expectation.stack);
                    activeTestCase.testResults.push(testResult);
       
                }

                // Log test case when done. This will get picked up by phantom and streamed to chutzpah.
                log({ type: "TestDone", testCase: activeTestCase });


            };

        }

        if (window.chutzpah.testMode) {
            jasmine.getEnv().addReporter(new ChutzpahJasmineReporter());
        }

        if (window.chutzpah.testMode === 'discovery') {
            // If discovery mode overwrite execute to not run the test
            var oldSpecExec = jasmine.Spec.prototype.execute;
            jasmine.Spec.prototype.execute = function(onComplete) {
                this.fn = function() {};
                oldSpecExec.call(this, onComplete);
            };
        }
    }

    function onPageLoaded() {
        console.log("!!_!! onPageLoaded");
    }


    try {
        chutzpah.runner(onInitialized, onPageLoaded, isJamineLoaded, onJasmineLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
