
![logo](https://github.com/Beriff/Cain/assets/58127431/ad5157e6-8e12-43b5-ae3e-0a5b65d0cefb)

Cavy is an object-oriented esoteric programming language, where everything is an object.

---
Hello world in Cavy:
```
System ctx {

    ^console->out: "Hello, world!".

}
```
interpret & run the code file with
```
cain file_name.cavy
```
## Everything is an object?
Right. Your code declared in {curly braces}, namespaces, object attribute keys, everything except the basic keywords (there's only two: `ctx` and `obj`). Of course this poses a problem: how can we define any data type if
everything we got is an object. The "primitive" data types are built from a special definition of natural numbers (slightly modified [von Neumann numerals](https://en.wikipedia.org/wiki/Set-theoretic_definition_of_natural_numbers) ):
```
{ {}: {} } = 0
{ {}: 0 } = { {}: { {}: {} } } = 1
{ {}: 1 } = 2
and so on...
```
They're called "raw numerals" in language, and by default, it is what the number literals return. A similar thing happens to the strings too - they're encoded naively as `{ index: charcode }`, so for example `{ 0: 65 }` is equal
to `"A"`. Yes, all of that is terribly inefficient.

### Protection mechanisms?
None. You can reassign any default object (including `System`), any object attribute.

## What's the point?
There is no point really. I wanted to see and design a language where everything is a modifiable object, and as it turns out, its possible. The language is not designed to be fast or sightreadable, but rather to treat everything interally as an object
