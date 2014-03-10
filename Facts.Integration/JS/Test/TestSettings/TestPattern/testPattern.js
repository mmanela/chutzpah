function TestClass() {
    
}

TestClass.prototype.test = function(name, callback) {
    QUnit.test(name, callback);
};

TestClass.prototype.testCase = function (name, callback) {
    QUnit.test(name, callback);
};

TestClass.prototype.registerTests = function(callback) {
    callback.call(this);
};

var testClass = new TestClass();

testClass.registerTests(function() {
    this.test("Pattern 1", function() {
        ok(true, "this test is fine");
    });

        this.testCase("Pattern 2", function () {
            ok(true, "this test is fine");
        });

});