/// <reference path="yuitest.js" />
// Example copied from http://developer.yahoo.com/yui/examples/yuitest/yt-simple-example.html

YAHOO.namespace("example.yuitest");

YAHOO.example.yuitest.DataTestCase = new YAHOO.tool.TestCase({

    //name of the test case - if not provided, one is auto-generated
    name: "Data Tests",

    //---------------------------------------------------------------------
    // setUp and tearDown methods - optional
    //---------------------------------------------------------------------

    /*
    * Sets up data that is needed by each test.
    */
    setUp: function () {
        this.data = {
            name: "yuitest",
            year: 2007,
            beta: true
        };
    },

    /*
    * Cleans up everything that was created by setUp().
    */
    tearDown: function () {
        delete this.data;
    },

    //---------------------------------------------------------------------
    // Test methods - names must begin with "test"
    //---------------------------------------------------------------------

    testName: function () {
        var Assert = YAHOO.util.Assert;

        Assert.isObject(this.data);
        Assert.isString(this.data.name);
        Assert.areEqual("yuitest", this.data.name);
    },

    testYear: function () {
        var Assert = YAHOO.util.Assert;

        Assert.isObject(this.data);
        Assert.isNumber(this.data.year);
        Assert.areEqual(2007, this.data.year);
    },

    testBeta: function () {
        var Assert = YAHOO.util.Assert;

        Assert.isObject(this.data);
        Assert.isBoolean(this.data.beta);
        Assert.isTrue(this.data.beta);
    }

});


YAHOO.example.yuitest.ArrayTestCase = new YAHOO.tool.TestCase({

    //name of the test case - if not provided, one is auto-generated
    name: "Array Tests",

    //---------------------------------------------------------------------
    // setUp and tearDown methods - optional
    //---------------------------------------------------------------------

    /*
    * Sets up data that is needed by each test.
    */
    setUp: function () {
        this.data = [0, 1, 2, 3, 4]
    },

    /*
    * Cleans up everything that was created by setUp().
    */
    tearDown: function () {
        delete this.data;
    },

    //---------------------------------------------------------------------
    // Test methods - names must begin with "test"
    //---------------------------------------------------------------------

    testPop: function () {
        var Assert = YAHOO.util.Assert;

        var value = this.data.pop();

        Assert.areEqual(4, this.data.length);
        Assert.areEqual(4, value);
    },

    testPush: function () {
        var Assert = YAHOO.util.Assert;

        this.data.push(5);

        Assert.areEqual(6, this.data.length);
        Assert.areEqual(5, this.data[5]);
    },

    testSplice: function () {
        var Assert = YAHOO.util.Assert;

        this.data.splice(2, 1, 6, 7);

        Assert.areEqual(6, this.data.length);
        Assert.areEqual(6, this.data[2]);
        Assert.areEqual(7, this.data[3]);
    }

});

YAHOO.example.yuitest.ExampleSuite = new YAHOO.tool.TestSuite("Example Suite");
YAHOO.example.yuitest.ExampleSuite.add(YAHOO.example.yuitest.DataTestCase);
YAHOO.example.yuitest.ExampleSuite.add(YAHOO.example.yuitest.ArrayTestCase);

YAHOO.util.Event.onDOMReady(function () {

    //create the logger
    var logger = new YAHOO.tool.TestLogger();

    //add the test suite to the runner's queue
    YAHOO.tool.TestRunner.add(YAHOO.example.yuitest.ExampleSuite);

    //run the tests
    YAHOO.tool.TestRunner.run();
});