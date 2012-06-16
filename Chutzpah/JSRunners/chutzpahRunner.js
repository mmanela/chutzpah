/*globals phantom, require, console*/
var chutzpah = {};

chutzpah.runner = function (areTestsComplete) {
    /// <summary>Executes a test suite and evaluates the results using the provided functions.</summary>
    /// <param name="areTestsComplete" type="Function">Function that returns true of false if the test suite should be considered complete and ready for evaluation.</param>
    'use strict';

    var page = require('webpage').create(),
        logs = [],
        errors = [],
        testFile = null,
        testMode = null,
        timeOut = null;

    function waitFor(testIfDone, timeOutMillis) {
        var maxtimeOutMillis = timeOutMillis,
            start = new Date().getTime(),
            isDone = false,
            interval;

        function intervalHandler() {
            if (!isDone && (new Date().getTime() - start < maxtimeOutMillis)) {
                isDone = testIfDone();
            } else {
                if (!isDone) {
                    phantom.exit(3); // Timeout
                } else {
                    clearInterval(interval);
                }
            }
        }

        interval = setInterval(intervalHandler, 100);
    }

    function wrap(txt) {
        return '#_#' + txt + '#_# ';
    }

    function captureLogMessage(message) {
        try {
            var obj = JSON.parse(message);
            if (!obj || !obj.type) throw "Unknown object";

            switch (obj.type) {
                case 'FileStart':

                    console.log(wrap(obj.type) + message);
                    break;

                case 'TestStart':
                case 'TestDone':
                    console.log(wrap(obj.type) + message);
                    break;

                case 'FileDone':
                    console.log(wrap(obj.type) + message);

                    var logsObj = { type: "Logs", Logs:logs };
                    var errorsObj = { type: "Errors", Errors: errors };
                    console.log(wrap(logsObj.type) + JSON.stringify(logsObj));
                    console.log(wrap(errorsObj.type) + JSON.stringify(errorsObj));
                    phantom.exit(obj.failed  > 0 ? 1 : 0);
                    break;

                default:
                    break;
            }
        }
        catch (e) {
            // The message was not a test status object so log as message
            logs.push({message: message});
        }
    }

    function onError(msg, stack) {
        errors.push({ message: msg, stack: stack });
    }

    function pageOpenHandler(status) {
        var waitCondition = function () { return page.evaluate(areTestsComplete); };

        if (status === 'success') {
            waitFor(waitCondition, timeOut);
        }
    }

    if (phantom.args.length === 0) {
        console.log('Error: too few arguments');
        phantom.exit();
    }

    testFile = phantom.args[0];
    testMode = phantom.args[1] || "execution";
    timeOut = parseInt(phantom.args[2]) || 10001;
    page.onConsoleMessage = captureLogMessage;
    page.onError = onError;

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