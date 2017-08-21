var module = module || {};
module.exports = module.exports || {};

var chutzpah = chutzpah || {};

chutzpah.getCommonFunctions = function (exit, updateEventTime) {

    var functions = {};

    function wrap(txt) {
        return '#_#' + txt + '#_# ';
    };

    functions.writeEvent = function (eventObj, json) {

        // Everytime we get an event update the startTime. We want timeout to happen
        // when were have gone quiet for too long
        updateEventTime();
        switch (eventObj.type) {
            case 'FileStart':
            case 'TestStart':
            case 'TestDone':
            case 'Log':
            case 'Error':
            case 'CoverageObject':
                console.log(wrap(eventObj.type) + json);
                break;

            case 'FileDone':
                console.log(wrap(eventObj.type) + json);
                exit(eventObj.failed > 0 ? 1 : 0);
                break;

            default:
                break;
        }
    };

    functions.captureLogMessage = function (message) {
        try {
            message = message.trim();
            var obj = JSON.parse(message);
            if (!obj || !obj.type) throw "Unknown object";
            functions.writeEvent(obj, message);

        }
        catch (e) {
            // The message was not a test status object so log as message
            functions.rawLog(message);
        }
    };

    functions.rawLog = function (message) {
        var log = { type: 'Log', log: { message: message } };
        functions.writeEvent(log, JSON.stringify(log));
    };

    functions.onError = function (msg, stack) {
        var error = { type: 'Error', error: { message: msg, stack: stack } };
        functions.writeEvent(error, JSON.stringify(error));
    };

    return functions;

};

module.exports.getCommonFunctions = chutzpah.getCommonFunctions;