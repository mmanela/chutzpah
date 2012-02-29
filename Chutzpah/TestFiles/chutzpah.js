/*
 * Chutzpah.js - Intercepts testing objects to help the Chutzpah test runner.
               - This script is placed right after the testing framework
 */


(function () {

    function runJasmine() {
        if (!chutzpah || chutzpah.testMode === 'execution') {
            // Only execute the tests when in execution mode
            jasmine.getEnv().execute();
        }
    }

    function setupQUnit() {
        if (window.chutzpah && window.chutzpah.testMode === 'discovery') {
            // In discovery mode override QUnit's functions
            window.chutzpah.testCases = [];
            window.chutzpah.currentModule = null;

            window.module = QUnit.module = function (name) {
                window.chutzpah.currentModule = name;
            };
            window.test = window.asyncTest = QUnit.test = QUnit.asyncTest = function (name) {
                window.chutzpah.testCases.push({ module: window.chutzpah.currentModule, name: name });
            };
        }
    }

    if (window.QUnit) {
        setupQUnit();
    }

    window.chutzpah = window.chutzpah || { };
    window.chutzpah.runJasmine = runJasmine;

} ());