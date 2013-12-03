## <reference path="../code.coffee" />

describe "general", ->
  it "A basic test", ->
    expect(true).toBeTruthy()
    value = "hello"
    expect("hello").toEqual value


describe "stringLib", ->
  it "will get vowel count", ->
    count = stringLib.vowels("hello")
    expect(count).toEqual 2


describe "mathLib", ->
  beforeEach ->
    console.log "beforeEach"

  it "will add 5 to number", ->
    res = mathLib.add5(10)
    expect(res).toEqual 15

  it "will multiply 5 to number", ->
    res = mathLib.mult5(10)
    expect(res).toEqual 55

