/*globals phantom, require, console*/
var chutzpah = {};

chutzpah.runner = function (testsComplete, testsEvaluator) {
    /// <summary>Executes a test suite and evaluates the results using the provided functions.</summary>
    /// <param name="testsComplete" type="Function">Function that returns true of false if the test suite should be considered complete and ready for evaluation.</param>
    /// <param name="testsEvaluator" type="Function">Function that returns a chutzpah.TestOutput containing the results of the test suite.</param>
    'use strict';

    var page = require('webpage').create(),
        logs = [],
        testFile = null,
        testMode = null,
        timeOut = null;

    function LogEntry(message, line, source) {
        this.message = message;
        this.line = line;
        this.source = source;
    }

    function waitFor(testFx, onReady, timeOutMillis) {
        var maxtimeOutMillis = timeOutMillis,
            start = new Date().getTime(),
            condition = false,
            interval;

        function intervalHandler() {
            if (!condition && (new Date().getTime() - start < maxtimeOutMillis)) {
                condition = testFx();
            } else {
                if (!condition) {
                    phantom.exit(3); // Timeout
                } else {
                    onReady();
                    clearInterval(interval);
                }
            }
        }

        interval = setInterval(intervalHandler, 100);
    }

    function addToLog(message, line, source) {
        logs.push(new LogEntry(message, line, source));
    }

    function pageOpenHandler(status) {
        var waitCondition = function () { return page.evaluate(testsComplete); },
            gatherTests = function () {
                var testSummary = page.evaluate(testsEvaluator);

                if (testSummary) {
                    testSummary.logs = logs;
                    console.log('#_#Begin#_#');
                    console.log(JSON.stringify(testSummary, null, 4));
                    console.log('#_#End#_#');
                    phantom.exit((parseInt(testSummary.failedCount, 10) > 0) ? 1 : 0);
                } else {
                    console.log("Unknown error");
                    phantom.exit(2); // Unkown error
                }
            };

        if (status !== 'success') {
            console.log('Unable to access network');
            phantom.exit();
        } else {
            waitFor(waitCondition, gatherTests, timeOut);
        }
    }

    if (phantom.args.length === 0) {
        console.log('Error: too few arguments');
        phantom.exit();
    }

    testFile = phantom.args[0];
    testMode = phantom.args[1] || "execution";
    timeOut = parseInt(phantom.args[2]) || 3001;
    page.onConsoleMessage = addToLog;

    page.onInitialized = function () {
        if (testMode === 'discovery') {
            page.evaluate(function () { window.chutzpah = { testMode: 'discovery' }; });
        }
        else {
            page.evaluate(function () { window.chutzpah = { testMode: 'execution' }; });
        }
    };

    page.open(testFile, pageOpenHandler);
};