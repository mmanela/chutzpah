/// <reference path="../code/encoding.coffee"/>

test("check encoding of strings", function() {
    var result = createString();
    equal(result, "æøå  πεπε", "Should be equal");
    notEqual(result, "Ã¦Ã¸Ã¥", "Shouldn't be equal");
});