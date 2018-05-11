/// <reference path="chutzpahRunner.js" />

(async function () {

    const functions = require('../mochaFunctions.js');
    const chutzpahRunner = require('./chutzpahRunner.js');

    try {
        await chutzpahRunner.runner(
            functions.onInitialized,
            functions.onPageLoaded,
            functions.isMochaLoaded,
            functions.onMochaLoaded,
            functions.isTestingDone);
        
    } catch (e) {
        throw new Error("Failed to run mochaRunner.js: " + e);
    }
})().catch(e => {
    console.error(e);
    process.exit(2);
});