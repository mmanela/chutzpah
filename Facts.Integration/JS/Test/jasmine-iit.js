
describe('normal', function () {
    it('not executed 1', function () {
        expect(true).toBe(false);
    });
    iit('executed 1', function () {
        expect(true).toBe(true);
    });
});

ddescribe('exclusive', function() {
    it('not executed 2', function() {
        expect(true).toBe(false);
    });
    iit('executed 2', function() {
        expect(true).toBe(true);
    });
    describe('nested exclusive', function() {
        iit('executed 3', function() {
            expect(true).toBe(true);
        });
    });
});

ddescribe('normal 2', function() {
    it('not executed 3', function() {
        expect(true).toBe(false);
    });
});
