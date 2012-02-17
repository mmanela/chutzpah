/// <reference path="chutzpah.js" />
/*globals phantom, chutzpah, window*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function testsComplete() {

        // If in discovery mode we know all tests at load time
        if (window.chutzpah.testMode === 'discovery') {
            return true;
        }

        var el = document.getElementById('qunit-testresult');

        if (el && el.innerText.match('completed')) {
            return true;
        }

        return false;
    }

    function testsEvaluator() {

        // Parses page to extract test results
        function getTestResults() {
            var testResults,
            testContainer,
            tests,
            i,
            length,
            test,
            moduleNode,
            result,
            failedAssert,
            hasNodes,
            expected,
            actual,
            message;

            testResults = {
                testCases: [],
                errors: [],
                failedCount: 0
            };

            testContainer = document.getElementById('qunit-tests');

            if (typeof testContainer !== 'undefined') {
                try {
                    tests = testContainer.children;
                    for (i = 0, length = tests.length; i < length; i += 1) {
                        test = tests[i];
                        moduleNode = test.querySelector('.module-name');

                        result = {
                            passed: test.className === 'pass',
                            name: test.querySelector('.test-name').innerHTML,
                            module: moduleNode !== null ? moduleNode.innerHTML : undefined
                        };

                        if (!result.passed) {
                            testResults.failedCount += 1;
                            failedAssert = test.querySelector('ol li.fail');
                            if (failedAssert) {
                                hasNodes = failedAssert.querySelector('*');

                                if (hasNodes) {
                                    expected = failedAssert.querySelector('.test-expected pre');
                                    actual = failedAssert.querySelector('.test-actual pre');
                                    message = failedAssert.querySelector('.test-message');

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

                        testResults.testCases.push(result);
                    }
                } catch (ex) {
                    testResults.errors.push(JSON.stringify(ex, null, 4));
                }

                return testResults;
            }

            return null;
        }

        // Get list of test cases without running them
        function getTestCases() {
            return { testCases: window.chutzpah.testCases };
        }

        if (window.chutzpah.testMode === 'discovery') {
            return getTestCases();
        }
        else {
            return getTestResults();
        }
    }

    try {
        chutzpah.runner(testsComplete, testsEvaluator);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
} ());
