## <reference path="../code/code.coffee" />

module "stringLib"
test "will get vowel count", ->
  count = stringLib.vowels("hello")
  equal count, 2, "We expect 2 vowels in hello"

module "mathLib"
test "will add 5 to number", ->
  res = mathLib.add5(10)
  equal res, 15, "should add 5"
