/// <reference path="qunit.d.ts" />
/// <reference path="jquery.d.ts" />
/// <chutzpah_reference path="../jquery-1.7.1.min.js" />

test("jquery with typescript", function () {
        var message = "a message";
        $("body").append("<div id='msg'></div>");

        $("#msg").text(message);

        equal($("#msg").text(), message, "message should get passed through");
});