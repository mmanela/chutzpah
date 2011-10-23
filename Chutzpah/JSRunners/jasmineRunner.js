/// <reference path="chutzpah.js" />
/*globals phantom, chutzpah*/

(function () {
    'use strict';

    phantom.injectJs('chutzpah.js');

    function testsComplete() {
        return !document.body.getElementsByClassName('running').length;
    }

    function testsEvaluator() {
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
            testCase;

        function attributeValue(node, attribute) {
            return node.attributes.getNamedItem(attribute).nodeValue;
        }

        testResults = {
            results: [],
            logs: [],
            errors: [],
            failedCount: 0
        };

        try {
            for (i = 0, length = specs.length; i < length; i += 1) {
                specNode = specs[i];
                nameNode = specNode.getElementsByClassName('description')[0];
                passed = attributeValue(specNode, 'class').match('passed') === 'passed';
                name = nameNode.innerText;
                fullName = attributeValue(nameNode, 'title');
                module = fullName.substr(0, (fullName.length - name.length) - 2);

                testCase = {
                    passed: passed,
                    name: name,
                    module: module
                };

                if (!passed) {
                    testResults.failedCount += 1;
                    messageNode = specNode.getElementsByClassName('resultMessage')[0];
                    testCase.message = messageNode.innerText;
                }

                testResults.results.push(testCase);
            }
        } catch (e) {
            testResults.errors.push(JSON.stringify(e, null, 4));
        }

        return testResults;
    }

    try {
        chutzpah.runner(testsComplete, testsEvaluator);
    } catch (e) {
        phantom.exit();
    }
}());