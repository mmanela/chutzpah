/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, jasmine*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function onInitialized() {
        console.log("!!_!! onInitialized Jasmine - v1");


        var _cachedWindowLoad = window.onload;

        function startJasmine() {
            
            console.log("!!_!! Starting Jasmine from window onload...");

            var jasmineEnv = jasmine.getEnv();
            var runner = jasmineEnv.currentRunner();

            // Check if runner hasn't been executed
            // If so, run it
            if (!runner.queue.running && runner.queue.index <= 0) {
                jasmineEnv.execute();
            }

        }

        window.onload = function () {
            if (_cachedWindowLoad) {
                _cachedWindowLoad();
            }


            if (window.chutzpah.usingModuleLoader) {
                console.log("!!_!! Test file is using module loader.");
                // Since we are using a module loader let the harness determine when its ready to run tests
                return;
            }
            


            startJasmine();
        };
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

        function patchDdescribeIitSupport() {
            if (window.ddescribeIitSupport) {
                window.ddescribeIitSupport.patch(jasmine.getEnv());
            }
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

        var ChutzpahJasmineReporter = function () {
            var self = this;

            self.reportRunnerStarting = function (runner) {
                // Must patch late since a HTML runner probably adds its on specFilter.
                patchDdescribeIitSupport();

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
        console.log("!!_!! onPageLoaded");
    }


    try {
        chutzpah.runner(onInitialized, onPageLoaded, isJamineLoaded, onJasmineLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
