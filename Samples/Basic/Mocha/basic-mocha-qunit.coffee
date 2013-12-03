## <reference path="../code.coffee" />
## <reference path="mocha.js" />
## <reference path="chai.js" />

expect = chai.expect

test "A basic test", ->
  expect(true, "this test is fine").to.be.ok
  value = "hello"
  expect("hello", "We expect value to be hello").to.equal(value)

suite "stringLib"
test "will get vowel count", ->
  count = stringLib.vowels("hello")
  expect(count, "We expect 2 vowels in hello").to.equal(2)

suite "mathLib"
test "will add 5 to number", ->
  res = mathLib.add5(10)
  expect(res, "should add 5").to.equal(15)

test "will multiply 5 to number", ->
  res = mathLib.mult5(10)
  expect(res, "should multiply by 5").to.equal(55)
