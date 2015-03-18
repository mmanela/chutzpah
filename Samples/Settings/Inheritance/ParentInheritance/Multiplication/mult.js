window.MyMath = window.MyMath || {};

MyMath.mult = function (x, y) {

	var acc = 0;
	for(var i = 0; i < y; i++){
		acc = MyMath.add(x, acc);
	}

	return acc;
}