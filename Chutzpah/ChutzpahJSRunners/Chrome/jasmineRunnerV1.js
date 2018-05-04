/// <reference path="chutzpahRunner.js" />

(async function () {

    const functions = require('../jasmineFunctionsV1.js');
    const chutzpahRunner = require('./chutzpahRunner.js');

    try {
        await chutzpahRunner.runner(
            functions.onInitialized,
            functions.onPageLoaded,
            functions.isJasmineLoaded,
            functions.onJasmineLoaded,
            functions.isTestingDone);
        
    } catch (e) {
        throw new Error("Failed to run jasmineRunnerV1.js: " + e);
    }
})().catch(e => {
    console.error(e);
    process.exit(2);
});