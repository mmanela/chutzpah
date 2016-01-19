/// <template path="directiveTemplate_comment.html" mode="script" id="template_comment" type="text/ng-template" /> 

describe("Unit Tests for the Template Direktive ", function() {
    var $rootScope, $compile;

    beforeEach(function() {
        //Load Main App
        module('main.app');

        inject(function(_$rootScope_, _$compile_) {
            $rootScope = _$rootScope_;
            $compile = _$compile_;
        });
    });

    describe('Tests Directive', function () {
        beforeEach(function () {
            inject(function ($templateCache) {
                //Find The Template in the current Unit Test HTML
                var templateFromComment = document.getElementById("template_comment").innerHTML;
                var templateFromJson = document.getElementById("template_json").innerHTML;
                //Set TemplateURL here and add the Template
                $templateCache.put("directiveTemplate_comment.html", templateFromComment);
                $templateCache.put("directiveTemplate_json.html", templateFromJson);
            });
        });

        it('contains the text template from reference comment', function () {
            var element = $compile("<div><div test-directive-comment></div>")($rootScope);
            $rootScope.$digest();

            expect(element.html()).toContain("<h1>I am included from a reference comment!</h1>");
        });

        it('contains the text template from json reference', function () {
            var element = $compile("<div><div test-directive-json></div>")($rootScope);
            $rootScope.$digest();

            expect(element.html()).toContain("<h1>I am included from the chutzpah.json file!</h1>");
        });
    });
})
