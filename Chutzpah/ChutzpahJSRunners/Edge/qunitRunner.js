/// <reference path="chutzpahRunner.js" />

module.exports = async function (params, callback) {

    const functions = require('../qunitFunctions.js');
    const chutzpahRunner = require('./chutzpahRunner.js');

    try {
        await chutzpahRunner.runner(params, callback,
            functions.onInitialized,
            functions.onPageLoaded,
            functions.isQunitLoaded,
            functions.onQUnitLoaded,
            functions.isTestingDone);

        callback(null, 'Node.js welcomes you');
        //chutzpah.runner(onInitialized, onPageLoaded, isQunitLoaded, onQUnitLoaded, isTestingDone);
    } catch (e) {
        throw new Error("Failed to run qunitRunner.js");
    }
};