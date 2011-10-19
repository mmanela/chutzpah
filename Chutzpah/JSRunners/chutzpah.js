/*globals phantom, WebPage, console*/
var chutzpah = {};

chutzpah.TestCase = function (passed, name, module) {
    /// <summary>Constructor for a single test result.</summary>
    /// <param name="passed" type="Boolean">Whether the test was successful.</param>
    /// <param name="name" type="String">Name of the test.</param>
    /// <param name="module" type="String">Module of the test.</param>
    /// <field name="state" type="String">State of the test.</field>
    /// <field name="name" type="String">Name of the test.</field>
    /// <field name="module" type="String">Module of the test.</field>
    /// <field name="message" type="String">Message associated with the test.</field>
    /// <field name="expected" type="String">Expected outcome of the test.</field>
    /// <field name="actual" type="String">Actual outcome of the test.</field>
    'use strict';

    this.passed = passed;
    this.name = name;
    this.module = module;
    this.message = '';
    this.expected = '';
    this.actual = '';
};

chutzpah.TestOutput = function (errors, failedCount) {
    /// <summary>Constructor for storing the results of a test evaluation.</summary>
    /// <param name="errors" type="Array" elementType="String">Array of error messages to seed the errors field.</param>
    /// <param name="failedCount" type="Number" integer="true">Number of failures counted.</param>
    /// <field name="errors" type="Array" elementType="String">Array of errors.</field>
    /// <field name="failedCount" type="Number" integer="true">Number of failures.</field>
    /// <field name="results" type="Array" elementType="chutzpah.TestCase">Array of test results.</field>
    /// <field name="logs" type="Array" elementType="String">Array of log messages.</field>
    'use strict';

    this.errors = errors;
    this.failedCount = failedCount;
    this.results = [];
    this.logs = [];

    this.addCase = function (result) {
        /// <summary>Adds a test result to the collection.</summary>
        /// <param name="result" type="chutzpah.TestCase">Result to add to the collection.</param>
        if (result instanceof chutzpah.TestCase) {
            this.logs.push(result);
        } else {
            throw new Error('Argument \'result\' must be an instance of chutzpah.TestCase.');
        }
    };
};

chutzpah.runner = function (testsComplete, testsEvaluator) {
    /// <summary>Executes a test suite and evaluates the results using the provided functions.</summary>
    /// <param name="testsComplete" type="Function">Function that returns true of false if the test suite should be considered complete and ready for evaluation.</param>
    /// <param name="testsEvaluator" type="Function">Function that returns a chutzpah.TestOutput containing the results of the test suite.</param>
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
            interval;

        function intervalHandler() {
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
        }

        interval = setInterval(intervalHandler, 100); //< repeat check every 250ms
    }

    function addToLog(message, line, source) {
        logs.push(new LogEntry(message, line, source));
    }

    function pageOpenHandler(status) {
        var waitCondition = function () { return page.evaluate(testsComplete); },
            gatherTests = function () {
                var testSummary = page.evaluate(testsEvaluator);

                if (testSummary instanceof chutzpah.TestOutput) {
                    testSummary.logs = testSummary.logs.concat(logs);
                    console.log('#_#Begin#_#');
                    console.log(JSON.stringify(testSummary, null, 4));
                    console.log('#_#End#_#');
                    phantom.exit((parseInt(testSummary.failedCount, 10) > 0) ? 1 : 0);
                } else {
                    phantom.exit();
                    throw new Error('Argument \'testEvaluator\' must return an instance of chutzpah.TestOutput.');
                }
            };

        if (status !== 'success') {
            console.log('Unable to access network');
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