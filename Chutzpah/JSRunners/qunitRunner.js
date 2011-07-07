/**
* Wait until the test condition is true or a timeout occurs. Useful for waiting
* on a server response or for a ui change (fadeIn, etc.) to occur.
*
* @param testFx javascript condition that evaluates to a boolean,
* it can be passed in as a string (e.g.: "1 == 1" or "$('#bar').is(':visible')" or
* as a callback function.
* @param onReady what to do when testFx condition is fulfilled,
* it can be passed in as a string (e.g.: "1 == 1" or "$('#bar').is(':visible')" or
* as a callback function.
* @param timeOutMillis the max amount of time to wait. If not specified, 3 sec is used.
*/
function waitFor(testFx, onReady, timeOutMillis) {
    var maxtimeOutMillis = timeOutMillis ? timeOutMillis : 3001, //< Default Max Timout is 3s
        start = new Date().getTime(),
        condition = false,
        interval = setInterval(function () {
            if ((new Date().getTime() - start < maxtimeOutMillis) && !condition) {
                // If not time-out yet and condition not yet fulfilled
                condition = (typeof (testFx) === "string" ? eval(testFx) : testFx()); //< defensive code
            } else {
                if (!condition) {
                    // If condition still not fulfilled (timeout but condition is 'false')
                    //console.log("'waitFor()' timeout");
                    phantom.exit(1);
                } else {
                    // Condition fulfilled (timeout and/or condition is 'true')
                    // console.log("'waitFor()' finished in " + (new Date().getTime() - start) + "ms.");
                    typeof (onReady) === "string" ? eval(onReady) : onReady(); //< Do what it's supposed to do once the condition is fulfilled
                    clearInterval(interval); //< Stop this interval
                }
            }
        }, 100); //< repeat check every 250ms
};


if (phantom.args.length === 0 || phantom.args.length > 2) {
    console.log('Error: too few arguments');
    phantom.exit();
}

var page = new WebPage();
var logs = []
// Route console messages
page.onConsoleMessage = function (msg, line, source) {
    logs.push({ message: msg, line: line, source: source });
    //console.log(msg);
    //console.log("Message: " + msg + "   Line: " + line + "   Source: " + source);
};

page.open(phantom.args[0], function (status) {
    if (status !== "success") {
        console.log("Unable to access network");
        phantom.exit();
    } else {
        waitFor(function () { // wait condition
            return page.evaluate(function () {
                var el = document.getElementById('qunit-testresult');
                if (el && el.innerText.match('completed')) {
                    return true;
                }
                return false;
            });
        },
            function () { // gather test results
                var testSummary = page.evaluate(function () {
                    var errors = [];

                    try {
                        var testRunningStatusContainer = document.getElementById('qunit-testresult');
                        var passed = testRunningStatusContainer.getElementsByClassName('passed')[0].innerHTML;
                        var total = testRunningStatusContainer.getElementsByClassName('total')[0].innerHTML;
                        var failed = testRunningStatusContainer.getElementsByClassName('failed')[0].innerHTML;
                    } catch (e) {
                        errors.push(JSON.stringify(e, null, 4));
                    }

                    var testResults = { 'results': [], 'logs': [], 'errors': errors, failedCount: failed };
                    var testContainer = document.getElementById('qunit-tests');


                    if (typeof testContainer !== 'undefined') {
                        try {
                            var tests = testContainer.children;
                            for (var i = 0; i < tests.length; i++) {
                                var test = tests[i];
                                var result = {};
                                result.state = test.className;
                                result.name = test.querySelector('.test-name').innerHTML;
                                var moduleNode = test.querySelector('.module-name');
                                if (moduleNode != null) {
                                    result.module = moduleNode.innerHTML;
                                }

                                if (result.state !== "pass") {
                                    var failedAssert = test.querySelector("ol li.fail");
                                    if (failedAssert) {
                                        var hasNodes = failedAssert.querySelector("*");

                                        if (hasNodes) {
                                            var expected = failedAssert.querySelector('.test-expected pre');
                                            var actual = failedAssert.querySelector('.test-actual pre');
                                            var message = failedAssert.querySelector('.test-message');

                                            if (message) {
                                                result.message = message.innerHTML;
                                            }

                                            if (expected) {
                                                result.expected = expected.innerHTML;
                                            }
                                            if (actual) {
                                                result.actual = actual.innerHTML;
                                            }
                                        } else {
                                            result.message = failedAssert.innerHTML;
                                        }
                                    }
                                }

                                testResults['results'].push(result);
                            }
                        } catch (e) {
                            errors.push(JSON.stringify(e, null, 4));
                        }

                        return testResults;
                    }
                });

                testSummary.logs = testSummary.logs.concat(logs);
                console.log("#_#Begin#_#");
                console.log(JSON.stringify(testSummary, null, 4));
                console.log("#_#End#_#");

                phantom.exit((parseInt(testSummary.failedCount, 10) > 0) ? 1 : 0);
            });

    }
});