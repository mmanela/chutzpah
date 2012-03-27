/// <reference path="chutzpah.js" />
/*globals phantom, chutzpah, jasmine*/

(function () {
    'use strict';

    phantom.injectJs('chutzpahRunner.js');

    function testsComplete() {

        // If in discovery mode we know all tests at load time
        if (window.chutzpah.testMode === 'discovery') {
            return true;
        }

        return !document.body.getElementsByClassName('running').length;
    }

    function testsEvaluator() {
        function getTestResults() {
            var i,
                length,
                specNode,
                nameNode,
                messageNode,
                testResults,
                specs = document.body.getElementsByClassName('spec'),
                passed,
                name,
                fullName,
                module,
                result;

            function attributeValue(node, attribute) {
                return node.attributes.getNamedItem(attribute).nodeValue;
            }

            testResults = {
                testCases: [],
                logs: [],
                errors: [],
                failedCount: 0
            };

            try {
                for (i = 0, length = specs.length; i < length; i += 1) {
                    specNode = specs[i];
                    nameNode = specNode.getElementsByClassName('description')[0];
                    passed = attributeValue(specNode, 'class').match(/passed/) ? true : false;
                    name = nameNode.innerText;
                    fullName = attributeValue(nameNode, 'title');
                    module = fullName.substr(0, (fullName.length - name.length) - 2);

                    result = {
                        passed: passed,
                        name: name,
                        module: module
                    };

                    if (!passed) {
                        testResults.failedCount += 1;
                        messageNode = specNode.getElementsByClassName('resultMessage')[0];
                        result.message = messageNode.innerText;
                    }

                    testResults.testCases.push(result);
                }
            } catch (ex) {
                testResults.errors.push(JSON.stringify(ex, null, 4));
            }

            return testResults;
        }

        function getFullSuiteName(suite) {
            var description = suite.description;
            if (suite.parentSuite) {
                description = getFullSuiteName(suite.parentSuite) + " " + description;
            }

            return description;
        }

        // Grab test case information from jasmine object model
        function getTestCases() {
            var testCases = [],
                suites = jasmine.getEnv().currentRunner_.suites_,
                suite,
                spec,
                i,
                j;
            
            for (i = 0; i < suites.length; i++) {
                suite = suites[i];
                var suiteName = getFullSuiteName(suite);
                for (j = 0; j < suite.specs_.length; j++) {
                    spec = suite.specs_[j];
                    testCases.push({ module: suiteName, name: spec.description });
                }
            }

            return { testCases: testCases };
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