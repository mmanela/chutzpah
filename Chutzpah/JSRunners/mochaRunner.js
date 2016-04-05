/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, mocha*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');
    
    function onInitialized() {
        console.log("!!_!! onInitialized");
        
        chutzpah.isTestingFinished = false;
        chutzpah.testCases = [];
    }

    function onPageLoaded() {
        console.log("!!_!! onPageLoaded");
    }

    function isMochaLoaded() {
        console.log("!!_!! isMochaLoaded");
        
        return window.mocha;
    }

    function onMochaLoaded() {
        console.log("!!_!! onMochaLoaded");
        

        function log(obj) {
            console.log(JSON.stringify(obj));
        }

        function discoverTests(suite) {
            suite.tests.forEach(function (test) {
                var testCase = { moduleName: suite.fullTitle(), testName: test.title };
                log({ type: "TestStart", testCase: testCase });
                log({ type: "TestDone", testCase: testCase });
            });

            suite.suites.forEach(discoverTests);

            chutzpah.isTestingFinished = true;
        }

        if (chutzpah.testMode === 'discovery') {
            console.log("!!_!! Starting Mocha in discovery mode");
            window.mocha.run = function () {
                log({ type: "FileStart" });
                discoverTests(window.mocha.suite);
                log({ type: "FileDone" });
            };
            return;
        }

        var chutzpahMochaReporter = function (runner) {
            var startTime = null,
                activeTestCase = null,
                passed = 0,
                failed = 0,
                skipped = 0;

            runner.on('start', function () {
                startTime = new Date();

                log({ type: "FileStart" });
            });

            
            runner.on('end', function () {
                if (window._Chutzpah_covobj_name && window[window._Chutzpah_covobj_name]) {
                    log({ type: "CoverageObject", object: JSON.stringify(window[window._Chutzpah_covobj_name]) });
                }

                log({
                    type: "FileDone",
                    timetaken: new Date() - startTime,
                    passed: passed,
                    failed: failed
                });

                chutzpah.isTestingFinished = true;
            });

            
            
            runner.on('suite', function (suite) {
                chutzpah.currentModule = suite.fullTitle();
            });

            runner.on('suite end', function (suite) {
                chutzpah.currentModule = null;
            });

            runner.on('test', function (test) {
                activeTestCase = {
                    moduleName: chutzpah.currentModule,
                    testName: test.title,
                    testResults: []
                };
                chutzpah.testCases.push(activeTestCase);
                log({ type: "TestStart", testCase: activeTestCase });
            });

            runner.on('test end', function (test) {
                if (test.pending) {
                    return;
                }
                activeTestCase.timetaken = test.duration;
                log({ type: "TestDone", testCase: activeTestCase });
            });
            
            
            //runner.on('hook', function(hook) { });
            //runner.on('hook end', function(hook) { });

            runner.on('pass', function (test) {
                passed++;
                activeTestCase.testResults.push({ passed: true });
            });

            runner.on('fail', function (test, err) {

                if ('hook' == test.type) {
                    log({ type: 'Error', error: { message: test.title, StackAsString: err.stack } });
                }
                else {
                    failed++;

                    activeTestCase.testResults.push({
                        passed: false,
                        stackTrace: err.stack ? err.stack : null,
                        message: err.message
                    });
                }
            });

            runner.on('pending', function (test) {
                skipped++;

                activeTestCase = {
                    moduleName: chutzpah.currentModule,
                    testName: test.title,
                    testResults: [],
                    skipped: true
                };
                chutzpah.testCases.push(activeTestCase);
                log({ type: "TestStart", testCase: activeTestCase });
                log({ type: "TestDone", testCase: activeTestCase });

            });
        };

        window.mocha.reporter(chutzpahMochaReporter);
    }

    function isTestingDone() {
        console.log("!!_!! isTestingDone: " + (chutzpah.isTestingFinished === true));
        return chutzpah.isTestingFinished === true;
    }

    try {
        chutzpah.runner(onInitialized, onPageLoaded, isMochaLoaded, onMochaLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
