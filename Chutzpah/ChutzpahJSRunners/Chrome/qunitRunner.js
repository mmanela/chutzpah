/// <reference path="chutzpahRunner.js" />

(async function () {

    const functions = require('../qunitFunctions.js');
    const chutzpahRunner = require('./chutzpahRunner.js');

    try {
        await chutzpahRunner.runner(
            functions.onInitialized,
            functions.onPageLoaded,
            functions.isQunitLoaded,
            functions.onQUnitLoaded,
            functions.isTestingDone);

    } catch (e) {
        throw new Error("Failed to run qunitRunner.js: " + e);
    }
})().catch(e => {
    console.error(e);
    process.exit(2);
});