/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, jasmine*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function isTestingDone() {
        return window.chutzpah.isTestingFinished === true;
    }
    
    function isJamineLoaded() {
        return window.jasmine;
    }
    
    function onJasmineLoaded() {
        function log(obj) {
            console.log(JSON.stringify(obj));
        }
        
        var activeTestCase = null;
        window.chutzpah.isTestingFinished = false;
        window.chutzpah.testCases = [];

        var ChutzpahJasmineReporter = function () {
            var self = this;

            self.reportRunnerStarting = function (runner) {

                // Testing began
                log({ type: "FileStart" });
            };

            self.reportRunnerResults = function (runner) {
                var res = jasmine.getEnv().currentRunner().results();
                log({ type: "FileDone", timetaken: 0, passed: res.passedCount, failed: res.failedCount });
                window.chutzpah.isTestingFinished = true;
            };

            self.reportSuiteResults = function (suite) { };

            self.reportSpecStarting = function (spec) {
                var suiteName = getFullSuiteName(spec.suite);
                var specName = spec.description;
                var newTestCase = { moduleName: suiteName, testName: specName, testResults: [] };
                window.chutzpah.testCases.push(newTestCase);
                activeTestCase = newTestCase;
                log({ type: "TestStart", testCase: activeTestCase });
            };

            self.reportSpecResults = function (spec) {
                var results = spec.results();
                var resultItems = results.getItems();
                for (var i = 0; i < resultItems.length; i++) {
                    var result = resultItems[i];
                    var testResult = {};
                    testResult.passed = result.passed();
                    testResult.message = result.message;
                    activeTestCase.testResults.push(testResult);
                }

                // Log test case when done. This will get picked up by phantom and streamed to chutzpah.
                log({ type: "TestDone", testCase: activeTestCase });
            };

            self.log = function () {
                var console = jasmine.getGlobal().console;
                if (console && console.log) {
                    if (console.log.apply) {
                        console.log.apply(console, arguments);
                    } else {
                        console.log(arguments);
                    }
                }
            };

            self.specFilter = function (spec) {
                return true;
            };

            function getFullSuiteName(suite) {
                var description = suite.description;
                if (suite.parentSuite) {
                    description = getFullSuiteName(suite.parentSuite) + " " + description;
                }

                return description;
            }

            return self;
        };

        if (window.chutzpah.testMode) {
            jasmine.getEnv().addReporter(new ChutzpahJasmineReporter());
        }

        if (window.chutzpah.testMode === 'discovery') {
            // If discovery mode overwrite execute to not run the test
            jasmine.Block.prototype.execute = function (onComplete) {
                onComplete();
            };
        }
    }

    function onPageLoaded() {

        var jasmineEnv = jasmine.getEnv();
        var runner = jasmineEnv.currentRunner();
        
        // Check if runner hasn't been executed
        // If so, run it
        if(!runner.queue.running && runner.queue.index <= 0) {
            jasmineEnv.execute();
        }
        
    }

    try {
        chutzpah.runner(onPageLoaded, isJamineLoaded, onJasmineLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
