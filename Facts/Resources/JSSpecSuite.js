/// <reference path="jsspec.js" />
// Example copied from http://jania.pe.kr/jsspec/demo.html

describe('Plus operator (just for example)', {
    'should concatenate two strings': function () {
        expect("Hello " + "World").should_be("Hello World");
    },
    'should add two numbers': function () {
        expect(1 + 2).should_be(3);
    }
})

describe('"Should match"s', {
    'Should match': function () {
        expect("Hello").should_match(/ell/);
    },
    'Should match 1': function () {
        expect("Hello").should_match(/x/);
    },
    'Should match 2': function () {
        expect([1, 2, 3]).should_match(/x/);
    },
    'Should not match 1': function () {
        expect("Hello").should_not_match(/ell/);
    },
    'Should not match 2': function () {
        expect([1, 2, 3]).should_not_match(/x/);
    }
})
describe('"Should include"s', {
    'Should include': function () {
        expect([1, 2, 3]).should_include(4);
    },
    'Should not include': function () {
        expect([1, 2, 3]).should_not_include(2);
    },
    'Should include / Non-array object': function () {
        expect(new Date()).should_include(4);
    },
    'Should not include / Non-array object': function () {
        expect(new Date()).should_not_include(4);
    }
})

describe('"Should have"s', {
    'String length': function () {
        expect("Hello").should_have(4, "characters");
    },
    'Array length': function () {
        expect([1, 2, 3]).should_have(4, "items");
    },
    'Object\'s item length': function () {
        expect({ name: 'Alan Kang', email: 'jania902@gmail.com', accounts: ['A', 'B'] }).should_have(3, "accounts");
    },
    'No match': function () {
        expect("This is a string").should_have(5, "players");
    },
    'Exactly': function () {
        expect([1, 2, 3]).should_have_exactly(2, "items");
    },
    'At least': function () {
        expect([1, 2, 3]).should_have_at_least(4, "items");
    },
    'At most': function () {
        expect([1, 2, 3]).should_have_at_most(2, "items");
    }
})
describe('"Should be empty"s', {
    'String': function () {
        expect("Hello").should_be_empty();
    },
    'Array': function () {
        expect([1, 2, 3]).should_be_empty();
    },
    'Object\'s item': function () {
        expect({ name: 'Alan Kang', email: 'jania902@gmail.com', accounts: ['A', 'B'] }).should_have(0, "accounts");
    }
})

describe('Failure messages', {
    'Should be (String)': function () {
        expect("Hello World").should_be("Good-bye world");
    },
    'Should have (Object\s item)': function () {
        expect({ name: 'Alan Kang', email: 'jania902@gmail.com', accounts: ['A', 'B'] }).should_have(3, "accounts");
    },
    'Should have at least': function () {
        expect([1, 2, 3]).should_have_at_least(4, "items");
    },
    'Should include': function () {
        expect([1, 2, 3]).should_include(4);
    },
    'Should match': function () {
        expect("Hello").should_match(/bye/);
    }
})

describe('"Should be"s', {
    'String mismatch': function () {
        expect("Hello world").should_be("Good-bye world");
    },
    'Array item mismatch': function () {
        expect(['ab', 'cd', 'ef']).should_be(['ab', 'bd', 'ef']);
    },
    'Array length mismatch': function () {
        expect(['a', 2, '4', 5]).should_be([1, 2, [4, 5, 6], 6, 7]);
    },
    'Undefined value': function () {
        expect("Test").should_be(undefined);
    },
    'Null value': function () {
        expect(null).should_be("Test");
    },
    'Boolean value 1': function () {
        expect(true).should_be(false);
    },
    'Boolean value 2': function () {
        expect(false).should_be_true();
    },
    'Boolean value 3': function () {
        expect(true).should_be_false();
    },
    'Number mismatch': function () {
        expect(1 + 2).should_be(4);
    },
    'Date mismatch': function () {
        expect(new Date(1979, 3, 27)).should_be(new Date(1976, 7, 23));
    },
    'Object mismatch 1': function () {
        var actual = { a: 1, b: 2 };
        var expected = { a: 1, b: 2, d: 3 };

        expect(actual).should_be(expected);
    },
    'Object mismatch 2': function () {
        var actual = { a: 1, b: 2, c: 3, d: 4 };
        var expected = { a: 1, b: 2, c: 3 };

        expect(actual).should_be(expected);
    },
    'Object mismatch 3': function () {
        var actual = { a: 1, b: 4, c: 3 };
        var expected = { a: 1, b: 2, c: 3 };

        expect(actual).should_be(expected);
    },
    'null should be null': function () {
        expect(null).should_be(null);
    },
    'null should not be undefined': function () {
        expect(null).should_be(undefined);
    },
    'null should not be null': function () {
        expect(null).should_not_be(null);
    },
    'empty array 1': function () {
        expect([]).should_be_empty();
        expect([1]).should_be_empty();
    },
    'empty array 2': function () {
        expect([1]).should_not_be_empty();
        expect([]).should_not_be_empty();
    }
})

describe('Equality operator', {
    'should work for different Date instances which have same value': function () {
        var date1 = new Date(1979, 03, 27);
        var date2 = new Date(1979, 03, 27);
        expect(date1).should_be(date2);
    }
})