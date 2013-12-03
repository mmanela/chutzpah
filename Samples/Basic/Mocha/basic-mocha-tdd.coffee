## <reference path="mocha.js" />
## <reference path="chai.js" />
## <reference path="../code.coffee" />

assert = chai.assert

test "A basic test", ->
  assert.ok true
  value = "hello"
  assert.equal value, "hello"

suite "stringLib", ->
    test "will get vowel count", ->
      count = stringLib.vowels "hello"
      assert.equal count, 2

suite "mathLib", ->
    test "will add 5 to number", ->
      res = mathLib.add5 10
      assert.equal res, 15

    test "will multiply 5 to number", ->
      res = mathLib.mult5 10
      assert.equal res, 55