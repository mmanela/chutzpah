/// <reference path="../code/code.js" />
/// <reference path="../code/style.css" />

module("Style Reference Copy Test");
test("Referenced styles should copy over", function () {
    var fontWeight = window.getComputedStyle(document.body, null).fontWeight;

    equal(fontWeight, "800");
});