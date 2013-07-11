/// <reference path="C:\Dev\chutzpah\Chutzpah\Compilers\TypeScript\lib.d.ts" />
/// <reference path="qunit.d.ts" />

test("Providing own lib.d.ts", function () {
        var message = "four";
        
        var length = message.length;

        equal(length, 4);
});