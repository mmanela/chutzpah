/// <reference path="chutzpahRunner.js" />

module.exports = async function (params, callback) {

    const functions = require('../jasmineFunctionsV1.js');
    const chutzpahRunner = require('./chutzpahRunner.js');

    try {
        await chutzpahRunner.runner(params, callback,
            functions.onInitialized,
            functions.onPageLoaded,
            functions.isJasmineLoaded,
            functions.onJasmineLoaded,
            functions.isTestingDone);
        
    } catch (e) {
        throw new Error("Failed to run jasmineRunnerV1.js: " + e);
    }
};