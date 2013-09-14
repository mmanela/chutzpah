(function () {
    'use strict';

    function onInitialized() {
        chutzpah.isTestingFinished = false;
        chutzpah.testCases = [];
    }

    function onPageLoaded() {
    }

    function isMochaLoaded() {
        return typeof global != 'undefined' && typeof global.mocha != 'undefined';
    }

    function onMochaLoaded() {
        function log(obj) {
            console.log(JSON.stringify(obj));
        }

        if (chutzpah.testMode === 'discovery') {
            // not quite sure here.
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
                    log({ type: "CoverageObject", object: window[window._Chutzpah_covobj_name] });
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
                activeTestCase.timetaken = test.duration;

                log({ type: "TestDone", testCase: activeTestCase });
            });

            //runner.on('hook', function(hook) { });
            //runner.on('hook end', function(hook) { });

            runner.on('pass', function (test) {
                passed++;
            });

            runner.on('fail', function (test, err) {
                failed++;
            });

            runner.on('pending', function (test) {
                skipped++;
            });
        };

        window.mocha.setup({ reporter: chutzpahMochaReporter });
    }

    function isTestingDone() {
        return chutzpah.isTestingFinished === true;
    }

    try {
        phantom.injectJs('chutzpahRunner.js');
        chutzpah.runner(onInitialized, onPageLoaded, isMochaLoaded, onMochaLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
}());
