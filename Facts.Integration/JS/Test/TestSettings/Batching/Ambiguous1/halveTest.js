test("Will work with zero", function(){
	var result = MyNumbers.halve(0);

	equal(result, 0);
});

test("Will halve a number", function(){
	var result = MyNumbers.halve(4);

	equal(result, 2);
});
