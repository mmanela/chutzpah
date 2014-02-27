## <reference path="mocha.js" />
## <reference path="chai.js" />
## <reference path="../code/code-coffee.coffee" />

expect = chai.expect

exports.basic = {
    "A basic test": ->
        expect(true).to.be.ok
        value = "hello"
        expect(value).to.equal "hello",
    "stringLib": {
        "will get vowel count": ->
            count = stringLib.vowels "hello"
            expect(count).to.equal 2
    },
    "mathLib": {
        beforeEach: ->
            console.log "beforeEach",
        "will add 5 to number": ->
            res = mathLib.add5 10
            expect(res).to.equal 15,
        "will multiply 5 to number": ->
            res = mathLib.mult5 10
            expect(res).to.equal 55
    }
}