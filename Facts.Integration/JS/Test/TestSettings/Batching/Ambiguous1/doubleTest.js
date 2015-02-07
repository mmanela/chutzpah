test("Will work with zero", function(){
	var result = MyNumbers.double(0);

	equal(result, 0);
});

test("Will double a number", function(){
	var result = MyNumbers.double(2);

	equal(result, 4);
});
