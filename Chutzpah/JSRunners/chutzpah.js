/*globals phantom, WebPage, console*/
var chutzpah = {};

chutzpah.TestResult = function (state, name, module) {
    'use strict';

    this.state = state;
    this.name = name;
    this.module = module;
    this.message = '';
    this.expected = '';
    this.actual = '';
};

chutzpah.TestResults = function (errors, failedCount) {
    'use strict';

    this.errors = errors;
    this.failedCount = failedCount;
    this.results = [];
    this.logs = [];

    this.addResult = function (result) {
        if (result instanceof chutzpah.TestResult) {
            this.logs.push(result);
        } else {
            throw new Error('Argument \'result\' must be an instance of chutzpah.TestResult.');
        }
    };
};

chutzpah.runner = function (testsComplete, testsEvaluator) {
    'use strict';

    var page = new WebPage(),
        logs = [];

    function LogEntry(message, line, source) {
        this.message = message;
        this.line = line;
        this.source = source;
    }

    function waitFor(testFx, onReady, timeOutMillis) {
        var maxtimeOutMillis = timeOutMillis || 3001, //< Default Max Timout is 3s
            start = new Date().getTime(),
            condition = false,
            interval,
            intervalHandler = function () {
                if ((new Date().getTime() - start < maxtimeOutMillis) && !condition) {
                    // If not time-out yet and condition not yet fulfilled
                    condition = testFx(); //< defensive code
                } else {
                    if (!condition) {
                        // If condition still not fulfilled (timeout but condition is 'false')
                        //console.log("'waitFor()' timeout");
                        phantom.exit(1);
                    } else {
                        // Condition fulfilled (timeout and/or condition is 'true')
                        // console.log("'waitFor()' finished in " + (new Date().getTime() - start) + "ms.");
                        onReady(); //< Do what it's supposed to do once the condition is fulfilled
                        clearInterval(interval); //< Stop this interval
                    }
                }
            };

        interval = setInterval(intervalHandler, 100); //< repeat check every 250ms
    }

    function addToLog(message, line, source) {
        logs.push(new LogEntry(message, line, source));
    }

    function pageOpenHandler(status) {
        var waitCondition = function () { return page.evaluate(testsComplete); },
            gatherTests = function () {
                var testSummary = page.evaluate(testsEvaluator);

                if (testSummary instanceof chutzpah.TestResults) {
                    testSummary.logs = testSummary.logs.concat(logs);
                    console.log("#_#Begin#_#");
                    console.log(JSON.stringify(testSummary, null, 4));
                    console.log("#_#End#_#");
                    phantom.exit((parseInt(testSummary.failedCount, 10) > 0) ? 1 : 0);
                } else {
                    phantom.exit();
                    throw new Error('Argument \'testEvaluator\' must return an instance of chutzpah.TestResults.');
                }
            };

        if (status !== "success") {
            console.log("Unable to access network");
            phantom.exit();
        } else {
            waitFor(waitCondition, gatherTests);
        }
    }

    if (phantom.args.length === 0 || phantom.args.length > 2) {
        console.log('Error: too few arguments');
        phantom.exit();
    }

    page.onConsoleMessage = addToLog;

    page.open(phantom.args[0], pageOpenHandler);
};