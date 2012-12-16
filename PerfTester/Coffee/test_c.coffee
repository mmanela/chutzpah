## <reference path="standardrefs.coffee" />
## <reference path="extendedrefs.coffee" />

describe "Some module C", () ->
    c = null
    x = null
    beforeEach () ->
        c = compute_c()
        x = c * c

    it "should be able to work", () ->
        expect(x).toEqual(9)

    describe "and also", () ->
        it "should be able to do magic", () ->
            z = compute_a_extended()
            y = compute_b_extended()
            expect(z*y*x).toEqual(10*20*9)