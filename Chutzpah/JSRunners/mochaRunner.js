/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, mocha*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function onInitialized() {
        console.log("!!_!! onInitialized");

        var _cachedWindowLoad = window.onload;

        function startMocha() {
            console.log("!!_!! Starting Mocha...");

            var mocha = new Mocha({ reporter: })

        }

        window.onload = function () {
            if (_cachedWindowLoad) {
                _cachedWindowLoad();
            }

            startMocha();
        };
    }

    function isTestingDone() {
        console.log("!!_!! isTestingDone");
        return window.chutzpah.isTestingFinished === true;
    }

    function isMochaLoaded() {
        console.log("!!_!! isMochaLoaded");
        return window.mocha;
    }

    function onMochaLoaded() {
        console.log("!!_!! onMochaLoaded");

        var mochaRunner = new MochaRunner();

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

        if (window.chutzpah.testMode) {
            mocha.getEnv().addReporter(new ChutzpahMochaReporter(instance));
        }

        if (window.chutzpah.testMode === 'discovery') {
            // If discovery mode overwrite execute to not run the test
            mocha.Block.prototype.execute = function (onComplete) {
                onComplete();
            };
        }
    }

    function onPageLoaded() {
        console.log("!!_!! onPageLoaded");
    }


    try {
        chutzpah.runner(onInitialized, onPageLoaded, isMochaLoaded, onMochaLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }

/*  Mocha Runner Events:
 *
 *   - `start`  execution started
 *   - `end`  execution complete
 *   - `suite`  (suite) test suite execution started
 *   - `suite end`  (suite) all tests (and sub-suites) have finished
 *   - `test`  (test) test execution started
 *   - `test end`  (test) test completed
 *   - `hook`  (hook) hook execution started
 *   - `hook end`  (hook) hook complete
 *   - `pass`  (test) test passed
 *   - `fail`  (test, err) test failed
 *   - `pending`  (test) test pending
*/

    var ChutzpahMochaReporter = (function() {
        function constructor(runner) {
            var self = this;
            
            self.runner = runner;

            runner.on('start', function() {
                fileStartTime = new Date().getTime();

                // Testing began
                log({ type: "FileStart" });
            });

            runner.on('pass', function(test){
                passes++;
                console.log('pass: %s', test.fullTitle());
            });

            runner.on('fail', function(test, err){
                failures++;
                console.log('fail: %s -- error: %s', test.fullTitle(), err.message);
            });

            runner.on('end', function(){
                console.log('end: %d/%d', passes, passes + failures);
                process.exit(failures);
            });

            self.reportRunnerResults = function (runner) {
                var res = mocha.getEnv().currentRunner().results();
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
                var console = mocha.getGlobal().console;
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



            return this;
        }

        constructor.prototype = {            
            
        };
        
        return constructor;
    }());

    var MochaRunner = (function() {
        function constructor() {
            this.activeTestCase = null;
            this.fileStartTime = null;
            this.testStartTime = null;
        }

        constructor.prototype = {
            log: function(obj) {
                console.log(JSON.stringify(obj));
            }
        };

        return constructor;
    }());
}());
