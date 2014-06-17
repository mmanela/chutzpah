/// <reference path="../code/exclude.js" chutzpah-exclude="true" />
/// <reference path="../code/exclude.js" chutzpahExclude="true" />

module("Excluded Reference Test");
test("Excluded references should not copy over", function () {
    ok(window.included !== true);
});