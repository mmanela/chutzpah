var SourceMaps;
(function (SourceMaps) {
    (function (Library) {
        var MathUtil = (function () {
            function MathUtil() {
            }
            MathUtil.prototype.Add = function (first, second) {
                return first + second;
            };

            MathUtil.prototype.IsEven = function (num) {
                if (num % 2 == 0) {
                    return true;
                }

                return false;
            };

            MathUtil.StaticAdd = function (first, second) {
                return first + second;
            };

            MathUtil.StaticIsEven = function (num) {
                if (num % 2 == 0) {
                    return true;
                }

                return false;
            };
            return MathUtil;
        })();
        Library.MathUtil = MathUtil;
    })(SourceMaps.Library || (SourceMaps.Library = {}));
    var Library = SourceMaps.Library;
})(SourceMaps || (SourceMaps = {}));
