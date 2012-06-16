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
    
    function writeEvent(eventObj, json) {
        switch (eventObj.type) {
            case 'FileStart':
            case 'TestStart':
            case 'TestDone':
            case 'Log':
            case 'Error':
                console.log(wrap(eventObj.type) + json);
                break;
                
            case 'FileDone':
                console.log(wrap(eventObj.type) + json);
                phantom.exit(eventObj.failed > 0 ? 1 : 0);
                break;
               
            default:
                break;
        }
    }

    function captureLogMessage(message) {
        try {
            var obj = JSON.parse(message);
            if (!obj || !obj.type) throw "Unknown object";
            writeEvent(obj, message);

        }
        catch (e) {
            // The message was not a test status object so log as message
            var log = { type: 'Log', log: { message: message } };
            writeEvent(log, JSON.stringify(log));
        }
    }

    function onError(msg, stack) {
        var error = { type: 'Error', error: { message: msg, stack: stack } };
        writeEvent(error, JSON.stringify(error));
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