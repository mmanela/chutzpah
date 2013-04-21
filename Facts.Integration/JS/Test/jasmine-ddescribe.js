
describe('normal', function () {
    it('not executed 1', function () {
        expect(true).toBe(false);
    });
    it('not executed 2', function () {
        expect(true).toBe(false);
    });
});

ddescribe('exclusive', function() {
    it('executed 1', function() {
        expect(true).toBe(true);
    });
    it('executed 2', function() {
        expect(true).toBe(true);
    });
    describe('nested exclusive', function() {
        it('executed 3', function() {
            expect(true).toBe(true);
        });
    });
});

describe('normal 2', function() {
    it('not executed 3', function() {
        expect(true).toBe(false);
    });
});
 