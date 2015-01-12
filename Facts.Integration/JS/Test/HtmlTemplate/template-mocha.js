/// <template path="template.tmpl.html"/>
/// <template path="template2.tmpl.html"/>
/// <reference path="../jquery-1.7.1.min.js" />
/// <reference path="mocha.js" />
/// <reference path="../chai.js" />

describe("Html Template Test - Mocha", function () {
    it("Will load html template", function () {
        
        // Grab template text, convert to html, append to body
        $($("#testTemplateId").text()).appendTo("body");

        var templateDiv = $("#testTemplateDiv");

        // Templatediv2 should already be in the body since it is not wrapped
        var templateDiv2 = $("#testTemplateDiv2");

        chai.expect(templateDiv.length).to.equal(1);
        chai.expect(templateDiv2.length).to.equal(1);
    });
});
