/// <reference path="../code.js" />


 QUnit.test("A basic test", function (assert) {
      assert.ok(true, "this test is fine");
      var value = "hello";
      assert.equal("hello", value, "We expect value to be hello");
  });

  QUnit.module("stringLib");

  QUnit.test("will get vowel count", function (assert) {
      var count = stringLib.vowels("hello");

      assert.equal(count, 2, "We expect 2 vowels in hello");
  });

  QUnit.module("mathLib");

  QUnit.test("will add 5 to number", function (assert) {
      var res = mathLib.add5(10);

      assert.equal(res, 15, "should add 5");
  });

  QUnit.test("will multiply 5 to number", function (assert) {
      var res = mathLib.mult5(10);

      assert.equal(res, 55, "should multiply by 5");
  });
