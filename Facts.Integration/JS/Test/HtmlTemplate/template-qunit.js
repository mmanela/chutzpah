/// <reference path="qunit.js" />
/// <template path="template.tmpl.html"/>
/// <reference path="../jquery-1.7.1.min.js" />

module("Html Template Test - QUnit");
test("Will load html template", function () {
    // Grab template text, convert to html, append to body
    $($("#testTemplateId").text()).appendTo("body");

    var templateDiv = $("#testTemplateDiv");

    equal(templateDiv.length, 1);
});
