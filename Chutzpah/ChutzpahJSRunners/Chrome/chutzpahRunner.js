var module = module || {};
module.exports = module.exports || {};

module.exports.runner = async (onInitialized, onPageLoaded, isFrameworkLoaded, onFrameworkLoaded, isTestingDone) => {

    const chutzpahCommon = require('../chutzpahFunctions.js');
    const chutzpahFunctions = chutzpahCommon.getCommonFunctions(process.exit, updateEventTime);

    var testFrameworkLoaded = false,
        attemptingToSetupTestFramework = false,
        testFile = null,
        testMode = null,
        timeOut = null,
        startTime = null,
        userAgent = null,
        ignoreResourceLoadingError = false,
        finalResult = 0,
        isRunningElevated = false,
        chromePath = null,
        browserArgs = null;

    startTime = new Date().getTime();

    testFile = process.argv[2];
    testMode = process.argv[3] || "execution";
    timeOut = parseInt(process.argv[4]) || 5001;

    if (process.argv.length > 5) {
        isRunningElevated = "true" === process.argv[5].toLowerCase();
    }

    if (process.argv.length > 6) {
        ignoreResourceLoadingError = "true" === process.argv[6].toLowerCase();
    }

    if (process.argv.length > 7) {
        chromePath = process.argv[7];
    }

    if (process.argv.length > 8) {
        userAgent = process.argv[8];
    }

    if (process.argv.length > 9) {
        browserArgs = process.argv[9];
    }

    function debugLog(msg) {
        //chutzpahFunctions.rawLog("!!_!! " + msg);
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
            if (loaded && (typeof loaded !== "string" || loaded === "true")) {
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
            let rawResult = await evaluate(isTestingDone);
            let result = rawResult && (typeof rawResult !== "string" || rawResult === "true");
            debugLog("@@@ waitCondition result: " + JSON.stringify(result));
            return result;
        };

        debugLog("Promise in pageOpenHandler");
        // Initialize startTime, this will get updated every time we receive 
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
      `.trim();
    }

    const puppeteer = require('puppeteer-core');
    const fs = require('fs');
    const path = require('path');

    function resolveChromePath() {
        if (canAccess(`${process.env.CHROME_PATH}`)) {
            return process.env.CHROME_PATH;
        }
        return undefined;
    }


    function getChromePaths() {
        const installations = [];
        const suffixes = [
            `${path.sep}Google${path.sep}Chrome SxS${path.sep}Application${path.sep}chrome.exe`,
            `${path.sep}Google${path.sep}Chrome${path.sep}Application${path.sep}chrome.exe`
        ];
        const prefixes = [
            process.env.LOCALAPPDATA, process.env.PROGRAMFILES, process.env['PROGRAMFILES(X86)']
        ].filter(Boolean);

        const customChromePath = resolveChromePath();
        if (customChromePath) {
            installations.push(customChromePath);
        }

        prefixes.forEach(prefix => suffixes.forEach(suffix => {
            const chromePath = path.join(prefix, suffix);
            if (canAccess(chromePath)) {
                installations.push(chromePath);
            }
        }));
        return installations;
    }

    function canAccess(file) {
        if (!file) {
            return false;
        }

        try {
            fs.accessSync(file);
            return true;
        } catch (e) {
            return false;
        }
    }


    function handleError(error) {
        var error;
        if (typeof (error) === 'object') {
            chutzpahFunctions.onError(error.message, error.stack);
        }
        else {
            chutzpahFunctions.onError(error);
        }
    }


    // Capture all uncaught exceptions and wrap before logging
    process.on('uncaughtException', handleError);

    try {
        let chromeExecutable = null;

        if (!chromePath) {
            const chromePaths = getChromePaths();
            if (chromePaths.length <= 0) {
                debugLog("Could not find chrome paths");
            }
            chromeExecutable = chromePaths[0];
        }
        else {
            chromeExecutable = chromePath;
        }

        chutzpahFunctions.rawLog("!!_!! Using Chrome Install : " + chromeExecutable);
        debugLog("Launch Chrome (" + chromeExecutable + "): Elevated= " + isRunningElevated);

        // If isRunningElevated, we need to turn off sandbox since it does not work with admin users
        var launchBrowserArges = isRunningElevated ? ["--no-sandbox"] : [];
        if (browserArgs) {
            launchBrowserArges.push(...browserArgs.trim().split(" "));
        }
        chutzpahFunctions.rawLog("!!_!! puppeteer browser args: " + JSON.stringify(launchBrowserArges));

        // If isRunningElevated, we need to turn off sandbox since it does not work with admin users
        const browser = await puppeteer.launch({
                headless: true, args: launchBrowserArges, executablePath: chromeExecutable
            });

        const page = await browser.newPage();

        try {
            await page.setBypassCSP(true);
        } catch (error) {
            // Older chromes won't support this so just ignore...
        }

        if (userAgent) {
            page.setUserAgent(userAgent);
        }

        const evaluate = async (func) => { return await page.evaluate(wrapFunctionForEvaluation(func)); };

        page.on('requestfinished', (async (request) => {
            chutzpahFunctions.rawLog("!!_!! Resource Received: " + request.url());
            await trySetupTestFramework(evaluate);
        }));

        page.on('requestfailed', ((request) => {
            let errorText = request.failure().errorText;
            if (!ignoreResourceLoadingError) {
                chutzpahFunctions.onError(errorText);
            }
            chutzpahFunctions.rawLog("!!_!! Resource Error for " + request.url() + " with error " + errorText);

        }));

        page.on('console', message => {
            if (message.type === 'error') {
                chutzpahFunctions.onError(message.text(), message.text());
            }
            else {
                chutzpahFunctions.captureLogMessage(message.text());
            }
        });

        page.on('error', error => {
            handleError(error);
        });

        page.on('pageerror', error => {
            handleError(error);
        });

        await page.evaluateOnNewDocument(getPageInitializationScript());

        debugLog("### Navigate..." + testFile);
        await page.goto(testFile, { waitUntil: "load" });

        debugLog("### loadEventFired");

        debugLog("### calling pageInitializedHandler");
        await pageInitializedHandler(evaluate);

        debugLog("### calling pageOpenHandler");
        finalResult = await pageOpenHandler(evaluate);
        debugLog("Just about done: " + finalResult);

    } catch (err) {
        chutzpahFunctions.rawLog("!!_!! Error: " + err);


        debugLog("Closing client");
        if (browser) {
            await browser.close();
        }

        process.exit(2);
    }


    debugLog("Closing client");
    if (browser) {
        await browser.close();
    }

    debugLog("Closed client");

    process.exit(finalResult);

};