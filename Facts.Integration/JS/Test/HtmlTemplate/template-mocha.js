/// <template path="template.tmpl.html"/>
/// <reference path="../jquery-1.7.1.min.js" />
/// <reference path="mocha.js" />
/// <reference path="../chai.js" />

describe("Html Template Test - Mocha", function () {
    it("Will load html template", function () {
        
        // Grab template text, convert to html, append to body
        $($("#testTemplateId").text()).appendTo("body");

        var templateDiv = $("#testTemplateDiv");

        chai.expect(templateDiv.length).to.equal(1);
    });
});
