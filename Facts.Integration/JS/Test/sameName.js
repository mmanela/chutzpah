/// <reference path="../code/sameName.js" />

test("Same Name Test", function () {
    var res = sameName();
    equals(res, "same", "We expect value to be 'same'");
});
