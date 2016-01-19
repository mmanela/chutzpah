
var app = angular.module("main.app", []);

app.controller("mainCtrl", function() {

});

app.directive("testDirectiveComment", function() {
    return {
      restrict: 'A',
      replace: true,
      templateUrl: "directiveTemplate_comment.html"
    }
});

app.directive("testDirectiveJson", function() {
    return {
      restrict: 'A',
      replace: true,
      templateUrl: "directiveTemplate_json.html"
    }
});