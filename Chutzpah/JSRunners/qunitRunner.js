/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, QUnit*/

(function () {
    'use strict';
    
    phantom.injectJs('chutzpahRunner.js');

    function isTestingDone() {

        return window.chutzpah.isTestingFinished === true;
    }
    
    function isQunitLoaded() {
        return window.QUnit;
    }
    
    function onQUnitLoaded() {
        function log(obj) {
            console.log(JSON.stringify(obj));
        }

        var activeTestCase = null;
        window.chutzpah.isTestingFinished = false;
        window.chutzpah.testCases = [];

        if (window.chutzpah.testMode === 'discovery') {
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

        QUnit.begin(function () {
            // Testing began
            log({ type: "FileStart" });
        });

        QUnit.testStart(function (info) {
            var newTestCase = { moduleName: info.module, testName: info.name, testResults: [] };
            window.chutzpah.testCases.push(newTestCase);
            activeTestCase = newTestCase;
            log({ type: "TestStart", testCase: activeTestCase });
        });

        QUnit.log(function (info) {
            if (info.result !== undefined) {
                var testResult = {};
                
                testResult.passed = info.result;
                QUnit.jsDump.multiline = false; // Make jsDump use single line
                testResult.actual = QUnit.jsDump.parse(info.actual);
                testResult.expected = QUnit.jsDump.parse(info.expected);
                testResult.message = (info.message || "") + "";
                
                activeTestCase.testResults.push(testResult);
            }
        });

        QUnit.testDone(function (info) {
            // Log test case when done. This will get picked up by phantom and streamed to chutzpah.
            log({ type: "TestDone", testCase: activeTestCase });
        });

        QUnit.done(function (info) {
            window.chutzpah.testingTime = info.runtime;

            log({ type: "FileDone", timetaken: info.runtime, passed: info.passed, failed: info.failed });
            window.chutzpah.isTestingFinished = true;
        });
    }
    
    function onPageLoaded() {}

    try {
        chutzpah.runner(onPageLoaded, isQunitLoaded, onQUnitLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
} ());
