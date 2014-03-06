var __extends = this.__extends || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    __.prototype = b.prototype;
    d.prototype = new __();
};
var StringBase = (function () {
    function StringBase(content) {
        this.content = content;
        this.vowels = "aeiou";
    }
    return StringBase;
})();

var StringPlus = (function (_super) {
    __extends(StringPlus, _super);
    function StringPlus(content) {
        _super.call(this, content);
        this.content = content;
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
})(StringBase);

var mathLib = {
    add5: function (a) {
        return a + 5;
    },
    mult5: function (a) {
        return a * 5;
    }
};
//# sourceMappingURL=code.js.map
