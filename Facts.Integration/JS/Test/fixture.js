window.QUnit.config.autostart = false;
test("A basic test", function () {
    equal(document.getElementById("important").innerHTML, "chutzpah");
});