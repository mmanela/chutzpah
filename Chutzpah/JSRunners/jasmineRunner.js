/// <reference path="chutzpah.js" />
/*globals chutzpah*/

(function () {
    'use strict';

    function attributeValue(node, attribute) {
        return node.attributes.getNamedItem(attribute).nodeValue;
    }

    function testsComplete() {
        var el = document.getElementsByClassName('runner')[0];
        return !attributeValue(el, 'class').match('running');
    }

    function testsEvaluator() {
        var i,
            length,
            specNode,
            nameNode,
            messageNode,
            testResults,
            specs = document.getElementsByClassName('spec'),
            passed,
            name,
            fullName,
            module,
            testCase;

        for (i = 0, length = specs.length; i < length; i += 1) {
            specNode = specs[i];
            nameNode = specNode.getElementsByClassName('description')[0];
            passed = attributeValue(specNode, 'class').match('passed');
            name = nameNode.innerText;
            fullName = attributeValue(nameNode, 'title');
            module = fullName.substr(0, (fullName.length - name.length) - 1);
            testCase = new chutzpah.TestCase(passed, name, module);

            if (!passed) {
                messageNode = specNode.getElementsByClassName('resultMessage')[0];
                testCase.message = messageNode.innerText;
            }
        }

        testResults = new chutzpah.TestOutput([], 0);

        return testResults;
    }

    chutzpah.runner(testsComplete, testsEvaluator);
}());