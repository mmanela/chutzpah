// This test validates that Chutzpah detects the Chutzpah.json file and uses its RootReferencePathMode setting to 
// change the behavior of a root reference path like <reference path="/stuff" /> to refer to start from the directory of the settings file
/// <reference path="/lib.js" />

module("RootReferencePathMode");
test("Settings File Directory", function () {
    ok(window.assertFileLoad);
});


