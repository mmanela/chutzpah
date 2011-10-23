/// <reference path="chutzpah.js" />
/*globals phantom, chutzpah*/

(function () {
    'use strict';

    phantom.injectJs('chutzpah.js');

    function testsComplete() {
        var el = document.getElementById('qunit-testresult');

        if (el && el.innerText.match('completed')) {
            return true;
        }

        return false;
    }

    function testsEvaluator() {
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
            results: [],
            logs: [],
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

                    testResults.results.push(result);
                }
            } catch (e) {
                testResults.errors.push(JSON.stringify(e, null, 4));
            }

            return testResults;
        }
    }

    try {
        chutzpah.runner(testsComplete, testsEvaluator);
    } catch (e) {
        phantom.exit();
    }
}());
