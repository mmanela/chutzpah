var module = module || {};
module.exports = module.exports || {};

const timers = require("timers");

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
        tryToFindChrome = false;

    startTime = new Date().getTime();;

    testFile = process.argv[2];
    testMode = process.argv[3] || "execution";
    timeOut = parseInt(process.argv[4]) || 5001;

    if (process.argv.length > 5) {
        isRunningElevated = "true" === process.argv[5].toLowerCase();
    }

    if (process.argv.length > 6) {
        ignoreResourceLoadingError = "true" === process.argv[6].toLowerCase();
    }

    if (process.argv.length > 8) {
        userAgent = process.argv[8];
    }

    function debugLog(msg) {
        chutzpahFunctions.rawLog("!!_!! " + msg);
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
            debugLog("=== loaded:" + JSON.stringify(loaded));
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

    function pageInitializedHandler(evaluate) {
        debugLog("pageInitializedHandler");
        evaluate(onInitialized);
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

    const jsdom = require("jsdom/lib/old-api.js");
    const fs = require('fs');
    const path = require('path');


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


    function captureLog(message) {
        //debugLog("captureLog: " + message);
        //debugLog("AA" + JSON.stringify(message));
        if (message.type === 'error') {
            chutzpahFunctions.onError(message, message);
        }
        else {
            chutzpahFunctions.captureLogMessage(message);
        }
    }


    let window = null;
    let evaluate = (func) => { return window.eval(wrapFunctionForEvaluation(func)); };

    try {

        const virtualConsole = jsdom.createVirtualConsole();
        virtualConsole.on("error", (message) => { debugLog("ErrorLog: " + message); handleError(message); });
        virtualConsole.on("jsdomError", (message) => { debugLog("jsdomError: " + message); handleError(message); });
        virtualConsole.on("warn", (message) => { captureLog(message); });
        virtualConsole.on("info", (message) => { captureLog(message); });
        virtualConsole.on("log", (message) => { captureLog(message); });


        const loadPagePromise = new Promise(function (resolve, reject) {

            jsdom.env({
                url: testFile,
                virtualConsole: virtualConsole,
                userAgent: userAgent, 
                resourceLoader: function (resource, callback) {
                    var href = resource.url.href;
                    return resource.defaultFetch((err, body) => {
                        if (err) {
                            let errorText = error.message;
                            if (!ignoreResourceLoadingError) {
                                chutzpahFunctions.onError(errorText);
                            }
                            chutzpahFunctions.rawLog("!!_!! Resource Error for " + href + " with error " + errorText);

                            return callback(err);
                        }

                        callback(null, body);

                        chutzpahFunctions.rawLog("!!_!! Resource Received: " + href);

                        // TODO: should I synchronize invocations of this?
                        trySetupTestFramework(evaluate);
                    });
                },
                features: {
                    FetchExternalResources: ["script", "frame", "iframe", "link", "img"],
                    ProcessExternalResources: ["script"],
                    SkipExternalResources: false
                },
                created: function (err, win) {

                    chutzpahFunctions.rawLog("!!_!! On JsDom ::created");

                    if (err !== null) {
                        return;
                    }
                    window = win;

                    // JSDom doesn't define these but we can use the global ones Node gives us.
                    window.setImmediate = timers.setImmediate;
                    window.clearImmediate = timers.clearImmediate;

                    debugLog("Setup stubs for JsDom");
                    window.eval(`
                        window.scrollTo = function() {};
                        HTMLCanvasElement.prototype.getContext = () => {
                         return {
                              fillStyle: null,
                              fillRect: function() {},
                              drawImage: function() {},
                              getImageData: function() {},
                              scale: function() {},
                            };
                        }
                    `);


                    debugLog("Evaling page initialization script");
                    window.eval(getPageInitializationScript());

                    pageInitializedHandler(evaluate);

                },
                done: function (err, win) {

                    chutzpahFunctions.rawLog("!!_!! On JsDom::loadDone");

                    if (err !== null) {
                        reject(err);
                    }

                    resolve();
                }
            });
        });


        debugLog("### Navigate...");
        await loadPagePromise;

        debugLog("### calling pageOpenHandler");
        finalResult = await pageOpenHandler(evaluate);
        debugLog("Just about done: " + finalResult);

        if (window) { window.close(); }

    } catch (err) {
        chutzpahFunctions.rawLog("!!_!! Error: " + err);
        if (window) { window.close(); }
        process.exit(2);
    }

    debugLog("Closed client");

    process.exit(finalResult);

};
