/// <reference path="http://code.jquery.com/jquery.min.js" />

asyncTest("An ajax call", function () {
    var r = $.ajax("http://www.bing.com");
    r.done(function () {
        ok(true);
        start();
    }).fail(function () {
        ok(false);
        start();
    });
});