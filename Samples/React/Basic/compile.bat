call npm install
call .\node_modules\.bin\babel.cmd --presets react,es2015 src/example.jsx --out-file src/example.js
call  .\node_modules\.bin\babel.cmd --presets react,es2015 test/example.test.jsx --out-file test/example.test.js