/// <reference path="../../libs/typings/angularjs/angular-mocks.d.ts" />
/// <reference path="../../libs/typings/angularjs/angular.d.ts" />
var Example;
(function (Example) {
    var Controller = /** @class */ (function () {
        function Controller($http) {
            var _this = this;
            this.DoSomeThing = function () {
                _this.$http.get("someAddress")
                    .success(function (response) {
                    _this.someResponse = response;
                }).error(function () {
                    _this.someResponse = -2;
                });
            };
            this.$http = $http;
            this.someResponse = -1;
        }
        return Controller;
    }());
    Example.Controller = Controller;
})(Example || (Example = {}));
//# sourceMappingURL=Controller.js.map