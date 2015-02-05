test("Will multiply two numbers", function(){
	var result = MyMath.mult(5,10);

	equal(result, 50);
});

test("Will multiply a number by 1", function(){
	var result = MyMath.mult(5,1);

	equal(result, 5);
});