## <reference path="mocha.js" />
## <reference path="chai.js" />
## <reference path="../code.coffee" />

expect = chai.expect

it "A basic test", ->
    expect(true).to.be.ok
    value = "hello"
    expect(value).to.equal "hello"
  
describe "stringLib", ->
    it "will get vowel count", ->
        count = stringLib.vowels "hello"
        expect(count).to.equal 2

describe "mathLib", ->
    beforeEach ->
        console.log "beforeEach"

    it "will add 5 to number", ->
        res = mathLib.add5 10
        expect(res).to.equal 15

    it "will multiply 5 to number", ->
        res = mathLib.mult5 10
        expect(res).to.equal 55