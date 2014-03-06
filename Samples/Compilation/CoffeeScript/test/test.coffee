## <reference path="../code/code.coffee" />

test "A basic test", ->
  ok true, "this test is fine"
  value = "hello"
  equal "hello", value, "We expect value to be hello"

module "stringLib"
test "will get vowel count", ->
  count = stringLib.vowels("hello")
  equal count, 2, "We expect 2 vowels in hello"

module "mathLib"
test "will add 5 to number", ->
  res = mathLib.add5(10)
  equal res, 15, "should add 5"

test "will multiply 5 to number", ->
  res = mathLib.mult5(10)
  equal res, 55, "should multiply by 5"
