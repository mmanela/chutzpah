test("Passed", function () {
    ok(true);
});

QUnit.skip("Skipped", function () {
    ok(false);
});

test("Failed", function () {
    ok(false);
});
