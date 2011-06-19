/// <reference path="../code/code.js" />


test("A script error", function () {
    ok(true);
});

test("A test error", function () {
    blah();
    ok(true);
});


test("A test throw error", function () {
    throw "BAD ERROR";
});

test("A code throw error", function () {
    throwError();
});
