/// <reference path="../../jasmine.d.ts" />

import screen = require('ui/screen');

describe("ui/screen", function () {
    it("will build display version", function () {
        var disp = screen.displayVersion;
        expect(disp).toEqual("Version: 8");
    });
});
