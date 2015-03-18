window.MyMath = window.MyMath || {};

MyMath.pow = function (x, y) {

	var acc = 1;
	for(var i = 0; i < y; i++){
		acc = MyMath.mult(x, acc);
	}

	return acc;
}