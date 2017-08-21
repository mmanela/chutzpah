var module = module || {};
module.exports = module.exports || {};

module.exports.runner = async (inputParams, callback, onInitialized, onPageLoaded, isFrameworkLoaded, onFrameworkLoaded, isTestingDone) => {

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

    console.log("@@@ timeout: " + timeOut);

    function updateEventTime() {
        console.log("### Updated startTime: " + startTime);
        startTime = new Date().getTime();
    }

    async function trySetupTestFramework(evaluate) {
        console.log("trySetupTestFramework");
        if (!testFrameworkLoaded) {

            console.log("checking isFrameworkLoaded ");
            var loaded = await evaluate(isFrameworkLoaded);
            if (loaded) {
                testFrameworkLoaded = true;
                console.log("calling onFrameworkLoaded");
                await evaluate(onFrameworkLoaded);
            }
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
            console.log("intervalHandler");
            var now = new Date().getTime();

            console.log(`@@@  isDone: ${isDone}, now: ${now}, startTime: ${startTime}, diff: ${now - startTime}`);
            if (!isDone && ((now - startTime) < maxtimeOutMillis)) {
                console.log("@@@ Checking if done...");
                isDone = await testIfDone();
                console.log("@@@ isDone: " + isDone);
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
            console.log("@@@ wait...: " + result);
            await wait(100);
            result = await intervalHandler();

            if (result >= 0) {
                console.log("Positive result, fin! " + result);
                return result;
            }
        }
    }

    async function pageOpenHandler(evaluate) {
        console.log("pageOpenHandler");

        var waitCondition = async () => {
            let result = await evaluate(isTestingDone);
            console.log("@@@ waitCondition result: " + JSON.stringify(result));
            return result.result && result.result.value;
        };

        console.log("Promise in pageOpenHandler");
        // Initialize startTime, this will get updated everytime we recieve 
        // content from the test framework
        updateEventTime();
        console.log("First trySetupTestFramework");
        await trySetupTestFramework(evaluate);


        console.log("Evaluate onPageLoaded");
        await evaluate(onPageLoaded);


        console.log("Calling waitFor...");
        return await waitFor(waitCondition, timeOut);
    }

    async function pageInitializedHandler(evaluate) {
        console.log("pageInitializedHandler");
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


    console.log("Launch Chrome");
    const launchedChrome = await chromeLauncher.launch({
        chromeFlags: ['--disable-gpu']
    });

    console.log("Get CDP");
    const client = await CDP({ port: launchedChrome.port });

    try {
        const { Network, Page, Runtime, Console, Security } = client;

        if (userAgent) {
            Network.setUserAgentOverride(userAgent);
        }

        const evaluate = async (func) => { return await Runtime.evaluate({ expression: wrapFunctionForEvaluation(func) }) };

        var chutzpahFunctions = chutzpahCommon.getCommonFunctions(function (status) { callback(null, status) }, updateEventTime);

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

        console.log("### Navigate...");
        await Page.navigate({ url: testFile });

        console.log("### After navigate");


        //console.log("### Wait for dom content loaded");
        //await Page.domContentEventFired();

        console.log("### Wait for page load event");
        await Page.loadEventFired();
        console.log("### loadEventFired");

        console.log("### calling pageInitializedHandler");
        await pageInitializedHandler(evaluate);

        console.log("### calling pageOpenHandler");
        let result = await pageOpenHandler(evaluate);
        callback(null, result);

    } catch (err) {
        console.error(err);
    } finally {
        await client.close();
    }

};