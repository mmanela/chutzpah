/// <reference path="chutzpah.js" />
/*globals chutzpah*/

(function () {
    'use strict';

    function testsComplete() {
        return false;
    }

    function testsEvaluator() {
        var results = new chutzpah.TestOutput([], 0);
        return results;
    }

    chutzpah.runner(testsComplete, testsEvaluator);
}());