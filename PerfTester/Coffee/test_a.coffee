## <reference path="standardrefs.coffee" />

describe "Some module A", () ->
    a = null
    b = null
    mult = null
    beforeEach () ->
        a = compute_a()
        b = compute_b()
        mult = a * b

    it "should be able to do stuff", () ->
        expect(mult).toEqual(2)
