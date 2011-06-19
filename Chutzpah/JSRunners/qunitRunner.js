function objectToString(o) {
    function escapeHTML(str) {
        var div = document.createElement('div');
        var text = document.createTextNode(str);
        div.appendChild(text);
        return div.innerHTML;
    }
    function isArray(obj) {
        return (toString.call(obj).indexOf("Array") > -1);
    }
    function isNumber(obj) {
        return (toString.call(obj).indexOf("Number") > -1);
    }
    function isString(obj) {
        return (toString.call(obj).indexOf("String") > -1);
    }
    function isObject(obj) {
        return (toString.call(obj).indexOf("Object") > -1);
    }
    var parse = function (_o) {
        var a = [], t;


        if (isNumber(_o)) {
            return _o;
        }
        else if (isString(_o)) {
            return '"' + escape(_o) + '"';
        }
        else if (isArray(_o)) {
            for (var i = 0; i < _o.length; i++) {
                a.push(arguments.callee(_o[i]));
            }
            return "[" + a.join(", ") + "]";
        }
        else {
            for (var p in _o) {
                if (_o.hasOwnProperty(p)) {
                    t = _o[p];
                    if (t && isObject(t)) {
                        a[a.length] = "\"" + p + "\"" + ": " + arguments.callee(t);
                    }
                    else {
                        a[a.length] = ["\"" + p + "\"" + ": " + arguments.callee(t)];
                    }
                }
            }
            return "{" + a.join(", ") + "}";
        }
    };
    return parse(o);
}

warnings = [];
errors = [];

if (console) {
    console.warn = function (m) {
        warnings.push(objectToString(m));
    };

    console.error = function (m) {
        errors.push(objectToString(m));
    };

    console.assert = function () {
        errors.push(objectToString(m));
    };

    console.info = function () { };
    console.count = function () { };
    console.debug = function () { };
    console.profileEnd = function () { };
    console.trace = function () { };
    console.dir = function () { };
    console.dirxml = function () { };
    console.time = function () { };
    console.profile = function () { };
    console.timeEnd = function () { };
    console.group = function () { };
    console.groupEnd = function () { };
}

window.onerror = function () { };

if (phantom.state.length === 0) {
    if (phantom.args.length === 0 || phantom.args.length > 2) {
        console.log("QUnit test runner for phantom.js");
        console.log('Usage: testrunner.js htmlTestFile.html');
        console.log('Accepts: http://example.com/file.html and file://some/path/test.html');
        phantom.exit();
    } else {
        phantom.state = "run-qunit";
        phantom.open(phantom.args[0]);
    }
} else {

    var done = false;
    setInterval(function () {
        if (phantom.state == 'finish') { return;  }
        var testRunningStatusContainer = document.getElementById('qunit-testresult');

        if (!testRunningStatusContainer) {
            phantom.exit(1);
            return;
        }

        if (document.querySelector("#qunit-tests li.running") != null) {
            return;
        }

        try {
            var passed = testRunningStatusContainer.getElementsByClassName('passed')[0].innerHTML;
            var total = testRunningStatusContainer.getElementsByClassName('total')[0].innerHTML;
            var failed = testRunningStatusContainer.getElementsByClassName('failed')[0].innerHTML;
        } catch (e) {
            console.error(e);
        }

        var testResults = { 'results': [], 'warnings': warnings, 'errors': errors };
        var testContainer = document.getElementById('qunit-tests');


        if (typeof testContainer !== 'undefined') {
            try {
                var tests = testContainer.children;
                for (var i = 0; i < tests.length; i++) {
                    var test = tests[i];
                    var result = {};
                    result.state = test.className;
                    result.name = test.querySelector('.test-name').innerHTML;
                    var moduleNode = test.querySelector('.module-name');
                    if (moduleNode != null) {
                        result.module = moduleNode.innerHTML;
                    }

                    if (result.state !== "pass") {
                        var failedAssert = test.querySelector("ol li.fail");
                        if (failedAssert) {
                            var hasNodes = failedAssert.querySelector("*");

                            if (hasNodes) {
                                var expected = failedAssert.querySelector('.test-expected pre');
                                var actual = failedAssert.querySelector('.test-actual pre');
                                var message = failedAssert.querySelector('.test-message');

                                if (message) {
                                    result.message = message.innerHTML;
                                }

                                if (expected) {
                                    result.expected = expected.innerHTML;
                                }
                                if (actual) {
                                    result.actual = actual.innerHTML;
                                }
                            } else {
                                result.message = failedAssert.innerHTML;
                            }
                        }
                    }

                    testResults['results'].push(result);
                }
            } catch (e) {
                console.error(e);
            }

            console.log("#_#Begin#_#");
            console.log(objectToString(testResults));
            console.log("#_#End#_#");

            if (parseInt(failed, 10) > 0) {
                phantom.exit(1);

            } else {
                phantom.exit(0);
            }
        }
    }, 100);
}