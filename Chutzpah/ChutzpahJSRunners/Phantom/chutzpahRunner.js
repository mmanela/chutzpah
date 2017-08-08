/*globals phantom, require, console*/
var chutzpah = chutzpah || {};

chutzpah.runner = function (onInitialized, onPageLoaded, isFrameworkLoaded, onFrameworkLoaded, isTestingDone) {
    /// <summary>Executes a test suite and evaluates the results using the provided functions.</summary>
    /// <param name="onInitialized" type="Function">Callback function which is called when the page initialized but not loaded.</param>
    /// <param name="onPageLoaded" type="Function">Callback function which is called when the page is loaded.</param>
    /// <param name="isFrameworkLoaded" type="Function">Function that returns true of false if the test framework has been loaded.</param>
    /// <param name="onFrameworkLoaded" type="Function">Callback function which is called when the test framework is loaded.</param>
    /// <param name="isTestingDone" type="Function">Function that returns true of false if the test suite should be considered complete and ready for evaluation.</param>
    'use strict';


    phantom.injectJs('../chutzpahFunctions.js');

    var page = require('webpage').create(),
        system = require("system"),
        testFrameworkLoaded = false,
        testFile = null,
        testMode = null,
        timeOut = null,
        startTime = null,
        userAgent = null,
        ignoreResourceLoadingError = false;

    if (system.args.length <= 1) {
        console.log('Error: too few arguments');
        phantom.exit();
    }

    testFile = system.args[1];
    testMode = system.args[2] || "execution";
    timeOut = parseInt(system.args[3]) || 5001;

    if (system.args.length > 4) {
        ignoreResourceLoadingError = "true" === system.args[4].toLowerCase();
    }

    if (system.args.length > 5) {
        userAgent = system.args[5];
    }

    function updateEventTime() {
        startTime = new Date().getTime();
    }

    var chutzpahFunctions = chutzpah.getCommonFunctions(phantom.exit, updateEventTime);


    function trySetupTestFramework() {
        if (!testFrameworkLoaded) {
            var loaded = page.evaluate(isFrameworkLoaded);
            if (loaded) {
                testFrameworkLoaded = true;
                page.evaluate(onFrameworkLoaded);
            }
        }
    }

    function waitFor(testIfDone, timeOutMillis) {
        var maxtimeOutMillis = timeOutMillis,
            isDone = false,
            interval;

        function intervalHandler() {
            var now = new Date().getTime();

            if (!isDone && (now - startTime < maxtimeOutMillis)) {
                isDone = testIfDone();
            } else {
                if (!isDone) {
                    phantom.exit(3); // Timeout
                } else {
                    clearInterval(interval);
                    phantom.exit(0);
                }
            }
        }

        interval = setInterval(intervalHandler, 100);
    }

    function pageOpenHandler(status) {
        var waitCondition = function () { return page.evaluate(isTestingDone); };

        if (status === 'success') {

            // Initialize startTime, this will get updated everytime we recieve 
            // content from the test framework
            updateEventTime();
            trySetupTestFramework();
            page.evaluate(onPageLoaded);
            waitFor(waitCondition, timeOut);
        }
        else {
            phantom.exit(2);
        }
    }

    page.onConsoleMessage = chutzpahFunctions.captureLogMessage;
    page.onError = chutzpahFunctions.onError;


    page.onInitialized = function () {
        if (testMode === 'discovery') {
            page.evaluate(function () {
                window.chutzpah = { testMode: 'discovery', phantom: true };
            });
        }
        else {
            page.evaluate(function () {
                window.chutzpah = { testMode: 'execution', phantom: true };
            });
        }

        page.evaluate(onInitialized);
    };

    page.onResourceReceived = function (res) {
        chutzpahFunctions.rawLog("!!_!! Resource Recieved: " + res.url);
        trySetupTestFramework();
    };

    page.onResourceError = function (res) {
        if (!ignoreResourceLoadingError) {
            chutzpahFunctions.onError(res.errorString);
        }
        chutzpahFunctions.rawLog("!!_!! Resource Error for " + res.url + " with error " + res.errorString);
    };


    page.onResourceTimeout = function (res) {
        if (!ignoreResourceLoadingError) {
            chutzpahFunctions.onError(res.errorString);
        }
        chutzpahFunctions.rawLog("!!_!! Resource Timeout for " + res.url + " with error " + res.errorString);
    };

    page.onAlert = function (msg) {
        chutzpahFunctions.rawLog("!!_!! Alert raised with message " + msg);
    };

    page.onConfirm = function (msg) {
        chutzpahFunctions.rawLog("!!_!! Confirm raised with message " + msg);
        return true;
    };


    page.onPrompt = function (msg, defaultVal) {
        chutzpahFunctions.rawLog("!!_!! Prompt raised with message " + msg);
        return defaultVal;
    };

    page.onClosing = function (closingPage) {
        chutzpahFunctions.rawLog("!!_!! Closing raised " + closingPage.url);
    };

    page.onUrlChanged = function (targetUrl) {
        chutzpahFunctions.rawLog("!!_!! Url Change " + targetUrl);
    };

    page.onFilePicker = function (oldFile) {
        chutzpahFunctions.rawLog("!!_!! File Picker raised " + oldFile);
        return oldFile;
    };

    // Since tests run inside of Phantom are not part of your websites domain
    // lets loosen remote resource security restrictions to help with some testing scenarios

    // Allows local files to make ajax calls to remote urls
    page.settings.localToRemoteUrlAccessEnabled = true; //(default false) 

    // Stops all security (for example you can access content in other domain IFrames)
    page.settings.webSecurityEnabled = false; //(default true)

    if (userAgent) {
        page.settings.userAgent = userAgent;
    }

    // Setup callback to be invoked from client using window.callPhantom
    page.onCallback = function (data) {

        if (!data || typeof data.Type !== "string") return null;

        switch (data.Type) {
            case "Eval":
                {
                    if (typeof data.Data === "string")
                        return eval(data.Data);
                    return null;
                }
        }
    }

    page.open(testFile, pageOpenHandler);
};