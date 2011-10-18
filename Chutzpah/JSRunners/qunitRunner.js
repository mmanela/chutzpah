/// <reference path="chutzpah.js" />
/*globals chutzpah*/

(function () {
    'use strict';

    function testsComplete() {
        var el = document.getElementById('qunit-testresult');

        if (el && el.innerText.match('completed')) {
            return true;
        }

        return false;
    }

    function testsEvaluator() {
        var errors = [],
            testRunningStatusContainer,
            failed,
            testResults,
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

        try {
            testRunningStatusContainer = document.getElementById('qunit-testresult');
            failed = testRunningStatusContainer.getElementsByClassName('failed')[0].innerHTML;
        } catch (argumentError) {
            errors.push(JSON.stringify(argumentError, null, 4));
        }

        testResults = new chutzpah.TestOutput(errors, failed);
        testContainer = document.getElementById('qunit-tests');

        if (typeof testContainer !== 'undefined') {
            try {
                tests = testContainer.children;
                for (i = 0, length = tests.length; i < length; i += 1) {
                    test = tests[i];
                    moduleNode = test.querySelector('.module-name');

                    result = new chutzpah.TestCase(
                        test.classname,
                        test.querySelector('.test-name').innerHTML,
                        moduleNode !== null ? moduleNode.innerHTML : undefined
                    );

                    if (result.state !== 'pass') {
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

                    testResults.addCase(result);
                }
            } catch (gatherError) {
                errors.push(JSON.stringify(gatherError, null, 4));
            }

            return testResults;
        }
    }

    chutzpah.runner(testsComplete, testsEvaluator);
}());