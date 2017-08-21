var module = module || {};
module.exports = module.exports || {};

module.exports.runner = async function (inputParams, callback, onInitialized, onPageLoaded, isFrameworkLoaded, onFrameworkLoaded, isTestingDone) {


    const chutzpahCommon = require('../chutzpahFunctions.js');

    var testFrameworkLoaded = false,
        testFile = null,
        testMode = null,
        timeOut = null,
        startTime = null,
        userAgent = null,
        ignoreResourceLoadingErrors = false;


    testFile = inputParams.fileUrl;
    testMode = inputParams.testMode || "execution";
    timeOut = parseInt(inputParams.timeOut) || 5001;
    ignoreResourceLoadingErrors = inputParams.ignoreResourceLoadingErrors;
    userAgent = inputParams.userAgent;

    function updateEventTime() {
        startTime = new Date().getTime();
    }

    function trySetupTestFramework(evaluate) {
        if (!testFrameworkLoaded) {
            var loaded = evaluate(isFrameworkLoaded);
            if (loaded) {
                testFrameworkLoaded = true;
                evaluate(onFrameworkLoaded);
            }
        }
    }

    function waitFor(exit, testIfDone, timeOutMillis) {
        var maxtimeOutMillis = timeOutMillis,
            isDone = false,
            interval;

        function intervalHandler() {
            var now = new Date().getTime();

            if (!isDone && (now - startTime < maxtimeOutMillis)) {
                isDone = testIfDone();
            } else {
                if (!isDone) {
                    exit(3); // Timeout
                } else {
                    clearInterval(interval);
                    exit(0);
                }
            }
        }

        interval = setInterval(intervalHandler, 100);
    }

    async function pageOpenHandler(evaluate) {
        var waitCondition = function () { return evaluate(isTestingDone); };

        return new Promise((resolve, reject) => {

            // Initialize startTime, this will get updated everytime we recieve 
            // content from the test framework
            updateEventTime();
            trySetupTestFramework(evaluate);
            evaluate(onPageLoaded);
            waitFor(resolve, waitCondition, timeOut);
        });


    }

    function pageInitializedHandler(evaluate) {

        if (testMode === 'discovery') {
            evaluate(function () {
                window.chutzpah = { testMode: 'discovery', phantom: true };
            });
        }
        else {
            evaluate(function () {
                window.chutzpah = { testMode: 'execution', phantom: true };
            });
        }

        evaluate(onInitialized);
    }


    const chromeLauncher = require('chrome-launcher');
    const CDP = require('chrome-remote-interface');

    const launchedChrome = await chromeLauncher.launch({
        chromeFlags: ['--disable-gpu']
    });

    const client = await CDP({ port: launchedChrome.port });

    try {
        const { Network, Page, Runtime, Console, Security } = client;

        if (userAgent) {
            Network.setUserAgentOverride(userAgent);
        }

        const evaluate = (func) => { return Runtime.evaluate({ expression: func }) };

        var chutzpahFunctions = chutzpahCommon.getCommonFunctions(function (status) { callback(null, status) }, updateEventTime);

        // Map from requestId to url
        const requestMap = {};

        Network.requestWillBeSent((params) => {
            requestMap[params.requestId] = params.request.url;
        });
        Network.responseReceived((params) => {
            const url = requestMap[params.requestId];
            chutzpahFunctions.rawLog("!!_!! Resource Recieved: " + url);
            trySetupTestFramework();
        });
        Network.loadingFailed((params) => {
            const url = requestMap[params.requestId];
            if (!ignoreResourceLoadingError) {
                chutzpahFunctions.onError(params.errorText);
            }
            chutzpahFunctions.rawLog("!!_!! Resource Error for " + url + " with error " + params.errorText);

        });
        Page.domContentEventFired((params) => {
            pageInitializedHandler(evaluate);
        });
        Console.messageAdded((params) => {
            if (params.message.level === 'error') {
                chutzpahFunctions.onError(params.message.text, params.message.text);
            }
            else {
                chutzpahFunctions.captureLogMessage(params.message.text);
            }
        });

        await Promise.all([Network.enable(), Page.enable(), Runtime.enable(), Console.enable()])

        await Page.navigate({ url: testFile });
        await Page.loadEventFired();

        let result = await pageOpenHandler(evaluate);
        callback(null, result);

    } catch (err) {
        console.error(err);
    } finally {
        await client.close();
    }

};