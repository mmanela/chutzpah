var module = module || {};
module.exports = module.exports || {};

module.exports.runner = async (inputParams, callback, onInitialized, onPageLoaded, isFrameworkLoaded, onFrameworkLoaded, isTestingDone) => {

    const chutzpahCommon = require('../chutzpahFunctions.js');

    var testFrameworkLoaded = false,
        attemptingToSetupTestFramework = false,
        testFile = null,
        testMode = null,
        timeOut = null,
        startTime = null,
        userAgent = null,
        ignoreResourceLoadingErrors = false,
        finalResult = 0;


    testFile = inputParams.fileUrl;
    testMode = inputParams.testMode || "execution";
    timeOut = parseInt(inputParams.timeOut) || 5001;
    ignoreResourceLoadingErrors = inputParams.ignoreResourceLoadingErrors;
    userAgent = inputParams.userAgent;

    function debugLog(msg) {
        console.log(msg);
    }

    function updateEventTime() {
        startTime = new Date().getTime();
    }

    async function trySetupTestFramework(evaluate) {
        debugLog("trySetupTestFramework");
        if (!testFrameworkLoaded && !attemptingToSetupTestFramework) {
            attemptingToSetupTestFramework = true;
            debugLog("checking isFrameworkLoaded ");
            var loaded = await evaluate(isFrameworkLoaded);
            if (loaded) {
                testFrameworkLoaded = true;
                debugLog("calling onFrameworkLoaded");
                await evaluate(onFrameworkLoaded);
            }

            attemptingToSetupTestFramework = false;
        }

    }

    async function wait(delay) {
        return new Promise(function (resolve, reject) {
            setTimeout(resolve, delay);
        });
    }

    async function waitFor(testIfDone, timeOutMillis) {
        let maxtimeOutMillis = timeOutMillis,
            isDone = false,
            result = -1;

        async function intervalHandler() {
            debugLog("intervalHandler");
            var now = new Date().getTime();

            if (!isDone && ((now - startTime) < maxtimeOutMillis)) {
                isDone = await testIfDone();
                return -1; // Not done, try again
            } else {
                if (!isDone) {
                    return 3; // Timeout
                } else {
                    return 0; // Done succesfully
                }
            }
        }


        while (result < 0) {
            debugLog("@@@ wait...: " + result);
            await wait(100);
            result = await intervalHandler();

            if (result >= 0) {
                debugLog("Positive result, fin! " + result);
                return result;
            }
        }
    }

    async function pageOpenHandler(evaluate) {
        debugLog("pageOpenHandler");

        var waitCondition = async () => {
            let result = await evaluate(isTestingDone);
            debugLog("@@@ waitCondition result: " + JSON.stringify(result));
            return result.result && result.result.value;
        };

        debugLog("Promise in pageOpenHandler");
        // Initialize startTime, this will get updated everytime we recieve 
        // content from the test framework
        updateEventTime();
        debugLog("First trySetupTestFramework");
        await trySetupTestFramework(evaluate);


        debugLog("Evaluate onPageLoaded");
        await evaluate(onPageLoaded);


        debugLog("Calling waitFor...");
        return await waitFor(waitCondition, timeOut);
    }

    async function pageInitializedHandler(evaluate) {
        debugLog("pageInitializedHandler");
        await evaluate(onInitialized);
    }

    function getPageInitializationScript() {
        if (testMode === 'discovery') {
            return "window.chutzpah = { testMode: 'discovery', phantom: true };";
        }
        else {
            return "window.chutzpah = { testMode: 'execution', phantom: true };";
        }
    }


    function wrapFunctionForEvaluation(func) {
        let str = '(' + func.toString() + ')()'

        // If the result is an instanceof of Promise, It's resolved in context of nodejs later.
        return `
        {
          let result = ${str};
          if (result instanceof Promise) {
            result;
          } 
          else {
            let json = JSON.stringify(result);
            json;
          }
        }
      `.trim()
    }


    const chromeLauncher = require('chrome-launcher');
    const CDP = require('chrome-remote-interface');


    debugLog("Launch Chrome");
    const launchedChrome = await chromeLauncher.launch({
        chromeFlags: ['--disable-gpu', '--headless']
    });

    debugLog("Get CDP");
    const client = await CDP({ port: launchedChrome.port });

    try {
        const { Network, Page, Runtime, Console, Security } = client;

        if (userAgent) {
            Network.setUserAgentOverride(userAgent);
        }

        const evaluate = async (func) => { return await Runtime.evaluate({ expression: wrapFunctionForEvaluation(func) }) };

        var chutzpahFunctions = chutzpahCommon.getCommonFunctions(function (status) { callback(null, status) }, updateEventTime, inputParams.onMessage);

        // Map from requestId to url
        const requestMap = {};

        Network.requestWillBeSent((params) => {
            requestMap[params.requestId] = params.request.url;
        });
        Network.responseReceived(async (params) => {
            const url = requestMap[params.requestId];
            chutzpahFunctions.rawLog("!!_!! Resource Recieved: " + url);
            await trySetupTestFramework(evaluate);
        });
        Network.loadingFailed((params) => {
            const url = requestMap[params.requestId];
            if (!ignoreResourceLoadingError) {
                chutzpahFunctions.onError(params.errorText);
            }
            chutzpahFunctions.rawLog("!!_!! Resource Error for " + url + " with error " + params.errorText);

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


        await Page.addScriptToEvaluateOnNewDocument({ source: getPageInitializationScript() });

        debugLog("### Navigate...");
        await Page.navigate({ url: testFile });

        debugLog("### After navigate");


        //debugLog("### Wait for dom content loaded");
        //await Page.domContentEventFired();

        debugLog("### Wait for page load event");
        await Page.loadEventFired();
        debugLog("### loadEventFired");

        debugLog("### calling pageInitializedHandler");
        await pageInitializedHandler(evaluate);

        debugLog("### calling pageOpenHandler");
        finalResult = await pageOpenHandler(evaluate);
        debugLog("Just about done: " + finalResult);

    } catch (err) {
        debugLog("Error: " + err);
        callback(err, null);
        return;
    }


    debugLog("Killing chrome");
    if (launchedChrome) {
        launchedChrome.kill();
    }

    debugLog("Closing client");
    if (client) {
        await client.close();
    }

    debugLog("Closed client");

    debugLog("Calling callback");
    callback(null, finalResult);

};