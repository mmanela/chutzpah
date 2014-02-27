throwError = ->
  throw "CODE ERROR"

stringLib = vowels: (a) ->
  count = 0
  i = 0

  while i < a.length
    count++  if "aeiou".indexOf(a[i]) > -1
    i++
  count

mathLib =
  add5: (a) ->
    a + 5

  mult5: (a) ->
    a * 5

errorThing = thing: ["thing" + someUndefinedVariable]