/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, jasmine*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function onInitialized() {
        // Add ddescribe and iit to window. If Jasmine has them, they will be overwritten.
        // Jasmine 2.0 removes globals, so this code will probably be affected at that point.
        window.ddescribe = function () {
            var jasmineEnv = jasmine.getEnv();
            var suite = jasmine.Env.prototype.describe.apply(jasmineEnv, Array.prototype.slice.call(arguments, 0));
            chutzpah.exclusive || (chutzpah.exclusive = { suites: {}, specs: {} });
            chutzpah.exclusive.suites[suite.getFullName()] = true;
            return suite;
        };
        window.iit = function () {
            var jasmineEnv = jasmine.getEnv();
            var spec = jasmine.Env.prototype.it.apply(jasmineEnv, Array.prototype.slice.call(arguments, 0));
            chutzpah.exclusive || (chutzpah.exclusive = { suites: {}, specs: {} });
            chutzpah.exclusive.specs[spec.getFullName()] = true;
            return spec;
        };
    }

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

        function recordStackTrace(trace) {
            var stack = trace && trace.stack || null;
            if (stack) {
                stack = stack.split('\n').slice(1).join('\n');
            }
            return stack;
        }

        function isExclusive(spec) {
            var hasExclusiveSpecs = Object.keys(window.chutzpah.exclusive.specs).length > 0,
                hasExclusiveSuites = Object.keys(window.chutzpah.exclusive.suites).length > 0;
            if (hasExclusiveSpecs) {
                // There are exclusive specs, so we run only those.
                return window.chutzpah.exclusive.specs[spec.getFullName()];
            }

            for (var suite = spec.suite; hasExclusiveSuites && suite; suite = suite.parentSuite) {
                if (window.chutzpah.exclusive.suites[suite.getFullName()])
                    return true;
            }
            return false;
        }

        var ChutzpahJasmineReporter = function () {
            var self = this;

            self.reportRunnerStarting = function (runner) {

                fileStartTime = new Date().getTime();

                // Testing began
                log({ type: "FileStart" });
            };

            self.reportRunnerResults = function (runner) {
                var res = jasmine.getEnv().currentRunner().results();
                var timetaken = new Date().getTime() - fileStartTime;
                logCoverage();
                log({ type: "FileDone", timetaken: timetaken, passed: res.passedCount, failed: res.failedCount });
                window.chutzpah.isTestingFinished = true;
            };

            self.reportSuiteResults = function (suite) { };

            self.reportSpecStarting = function (spec) {
                testStartTime = new Date().getTime();
                var suiteName = getFullSuiteName(spec.suite);
                var specName = spec.description;
                var newTestCase = { moduleName: suiteName, testName: specName, testResults: [] };
                window.chutzpah.testCases.push(newTestCase);
                activeTestCase = newTestCase;
                log({ type: "TestStart", testCase: activeTestCase });
            };

            self.reportSpecResults = function (spec) {
                var results = spec.results();
                if (results.skipped) {
                    return;
                }
                var timetaken = new Date().getTime() - testStartTime;
                activeTestCase.timetaken = timetaken;
                var resultItems = results.getItems();
                for (var i = 0; i < resultItems.length; i++) {
                    var result = resultItems[i];
                    var testResult = {};

                    // Check the existance of result.passed, don't call it!
                    if (result.passed) {
                        // result.passed() may return (true/false) or (1,0) but we want to only return boolean
                        testResult.passed = result.passed() ? true : false;
                        testResult.message = result.message;
                        testResult.stackTrace = recordStackTrace(result.trace);
                        activeTestCase.testResults.push(testResult);
                    } else {
                        // Not an ExpectationResult, probably a MessageResult. Treat as any other log message.
                        log({ type: 'Log', log: { message: result.toString() } });
                    }
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
                if (window.chutzpah.exclusive) {
                    return isExclusive(spec);
                }
                return self.origSpecFilter.call(jasmine.getEnv(), spec);
            };

            function getFullSuiteName(suite) {
                var description = suite.description;
                if (suite.parentSuite) {
                    description = getFullSuiteName(suite.parentSuite) + " " + description;
                }

                return description;
            }

            // Jasmine doesn't call a reporter's specFilter function, so we have to patch it in.
            self.origSpecFilter = jasmine.getEnv().specFilter;
            jasmine.getEnv().specFilter = self.specFilter;

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
        var _cachedWindowLoad = window.onload;
        window.onload = function () {
            if (_cachedWindowLoad) {
                _cachedWindowLoad();
            }
            
            var jasmineEnv = jasmine.getEnv();
            var runner = jasmineEnv.currentRunner();

            // Check if runner hasn't been executed
            // If so, run it
            if (!runner.queue.running && runner.queue.index <= 0) {
                jasmineEnv.execute();
            }
        };
    }

    try {
        chutzpah.runner(onInitialized, onPageLoaded, isJamineLoaded, onJasmineLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
