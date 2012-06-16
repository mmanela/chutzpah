/*
 * Chutzpah.js - Intercepts testing objects to help the Chutzpah test runner.
               - This script is placed right after the testing framework
 */


(function () {

    function runJasmine() {
        if (!window.chutzpah || !window.chutzpah.testMode || window.chutzpah.testMode === 'execution') {
            // Only execute the tests when in execution mode
            jasmine.getEnv().execute();
        }
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

            log({ type: "FileDone", timetaken: info.runtime, failed: info.failed, passed: info.passed });
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