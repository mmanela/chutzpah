var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var StringBase = /** @class */ (function () {
    function StringBase(content) {
        this.content = content;
        this.vowels = "aeiou";
    }
    return StringBase;
}());
var StringPlus = /** @class */ (function (_super) {
    __extends(StringPlus, _super);
    function StringPlus(content) {
        var _this = _super.call(this, content) || this;
        _this.content = content;
        return _this;
    }
    StringPlus.prototype.countVowels = function () {
        var count = 0;
        for (var i = 0; i < this.content.length; i++) {
            if (this.vowels.indexOf(this.content[i]) > -1) {
                count++;
            }
        }
        return count;
    };
    return StringPlus;
}(StringBase));
var mathLib = {
    add5: function (a) {
        return a + 5;
    },
    mult5: function (a) {
        return a * 5;
    }
};
//# sourceMappingURL=code.js.map