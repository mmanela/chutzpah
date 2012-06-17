/*
 * Chutzpah.js - Intercepts testing objects to help the Chutzpah test runner.
               - This script is placed right after the testing framework
 */


(function () {

    function runJasmine() {

        var activeTestCase = null;
        window.chutzpah.isRunning = true;
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
                window.chutzpah.isRunning = false;
            };

            self.reportSuiteResults = function (suite) {};

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
        
        if(window.chutzpah && window.chutzpah.testMode) {
            jasmine.getEnv().addReporter(new ChutzpahJasmineReporter());
        }
        
        if (window.chutzpah && window.chutzpah.testMode === 'discovery') {
            // If discovery mode overwrite execute to not run the test
            jasmine.Block.prototype.execute = function (onComplete) {
                onComplete();
            };
        }

        // Run Jasmine
        jasmine.getEnv().execute();
    }


    function setupQUnit() {
        var activeTestCase = null;
        window.chutzpah.isRunning = true;
        window.chutzpah.testCases = [];
        
        if (window.chutzpah && window.chutzpah.testMode === 'discovery') {
            window.chutzpah.currentModule = null;

            // In discovery mode override QUnit's functions

            window.module = QUnit.module = function (name) {
                window.chutzpah.currentModule = name;
            };
            window.test = window.asyncTest = QUnit.test = QUnit.asyncTest = function (name) {
                var testCase = { moduleName: window.chutzpah.currentModule, testName: name };
                log({ type: "TestDone", testCase: testCase });
            };
        }
        
        QUnit.begin(function() {
            // Testing began
            log({ type: "FileStart" });
        });

        QUnit.testStart(function(info) {
            var newTestCase = { moduleName: info.module, testName: info.name, testResults: [] };
            window.chutzpah.testCases.push(newTestCase);
            activeTestCase = newTestCase;
            log({ type: "TestStart", testCase: activeTestCase });
        });

        QUnit.log(function(info) {
            if (info.result !== undefined) {
                var testResult = { };
                testResult.passed = info.result;
                testResult.actual = info.actual;
                testResult.expected = info.expected;
                testResult.message = info.message;

                activeTestCase.testResults.push(testResult);
            }
        });

        QUnit.testDone(function(info) {
            // Log test case when done. This will get picked up by phantom and streamed to chutzpah.
            log({ type: "TestDone", testCase: activeTestCase });
        });

        QUnit.done(function(info) {
            window.chutzpah.testingTime = info.runtime;

            log({ type: "FileDone", timetaken: info.runtime, passed: info.passed, failed: info.failed});
            window.chutzpah.isRunning = false;
        });
        

    }
    
    function log(obj) {
        console.log(JSON.stringify(obj));
    }

    if (window.QUnit) {
        setupQUnit();
    }

    window.chutzpah = window.chutzpah || { };
    window.chutzpah.runJasmine = runJasmine;

} ());