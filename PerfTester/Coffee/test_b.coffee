## <reference path="standardrefs.coffee" />

describe "Some module B", () ->
    b = null
    c = null
    sum = null
    beforeEach () ->
        b = compute_b()
        c = compute_c()
        sum = b + c

    it "should be able to do something else", () ->
        expect(sum).toEqual(5)