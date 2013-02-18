/// <reference path="../code/exclude.js" chutzpah-exclude="true" />

module("Excluded Reference Test");
test("Excluded references should not copy over", function () {
    ok(window.included !== true);
});