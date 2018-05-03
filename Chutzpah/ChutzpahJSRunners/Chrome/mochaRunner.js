/// <reference path="chutzpahRunner.js" />

module.exports = async function (params, callback) {

    const functions = require('../mochaFunctions.js');
    const chutzpahRunner = require('./chutzpahRunner.js');

    try {
        await chutzpahRunner.runner(params, callback,
            functions.onInitialized,
            functions.onPageLoaded,
            functions.isMochaLoaded,
            functions.onMochaLoaded,
            functions.isTestingDone);
        
    } catch (e) {
        throw new Error("Failed to run mochaRunner.js: " + e);
    }
};