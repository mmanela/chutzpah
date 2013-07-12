/// <template path="template.tmpl.html"/>
/// <reference path="../jquery-1.7.1.min.js" />
/// <reference path="jasmine.js" />

describe("Html Template Test - Jasmine", function () {
    it("Will load html template", function () {
        
        // Grab template text, convert to html, append to body
        $($("#testTemplateId").text()).appendTo("body");

        var templateDiv = $("#testTemplateDiv");

        expect(templateDiv.length).toEqual(1);
    });
});
