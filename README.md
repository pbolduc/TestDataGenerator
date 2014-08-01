TestDataGenerator
=================

Generate dummy data which can be used for testing, You provide it with a pattern which you want to generate and it will create 
random data to match that pattern.

## Placeholders
The 'GenerateFromTemplate' method allows you to provide a string containing placeholders values, these placeholders are 
comprised of 1 or more symbols representing the desired output characters.  Note that placeholders are wrapped in double 
parenthesis e.g. `Hi ((Lvlv))` where `((Lvlv))` is the placeholder pattern containing the symbols `Lvlv`.

####For example
- `"This is a ((LL)) string"` will produce something similar to `"This is a AQ string"` where `'AQ'` is randomly generated.
- `"This is a (([XX]{19})) string"` will produce something similar to `"This is a 3698145258142562124 string"` where `'3698145258142562124'` is randomly generated.

## Symbols
The pattern is as follows:
- `*` - An upper-case or lower-case letter or number.
- `L` - An upper-case Letter.
- `l` - A lower-case letter.
- `V` - An upper-case Vowel.
- `v` - A lower-case vowel.
- `C` - An upper-case Consonant.
- `c` - A lower-case consonant.
- `X` - Any number, 0-9.
- `x` - Any number, 1-9.

####For example:
- Individual symbols can be repeated a specific number of times using the syntax `L{10}` which will generate 10 upper case letters.
- Individual symbols can be repeated a random number of times using the syntax `L{10,20}` which will generate between 10 and 20 upper case letters.
- 1 or more Symbols can be combined into patterns by wrapping them in square brackets e.g. `[*LX]`.
- Patterns can be repeated a specific number of times using the syntax `[LX]{10}` which will generate 10 repeated letter-number pairs e.g. 'G5L6I0O4F4K5A0O7W9H7'.
- Patterns can be repeated a random number of times using the syntax `LX{10,20}` which will generate between 10 and 20 repeated letter-number 
pairs e.g. 'S9B7E9P3F8F1L5I2R5B7J7H0P8X4K1I7'.


## Commandline Tool
You can use the `tdg.exe` application to generate test data from the command line.  You can provide templates directly from the command line or from a file and 
the tool also supports exporting the generated output to either the command line or another file.

####Example usage:
- Generate 100 SSN like values and output to console window
  - `tdg -t "((Xxx-xx-xxxx))" -c 100`
- Generate 100 strings with random name like values and output to file 
  - `tdg -t "Hi there ((Lvlv Lvllvvllv)) how are you doing?" -c 100 -o C:\test1.txt`
- Combine several placeholders to produce more complicated content
  - `tdg -t "Hi there ((L[v]{0,2}[l]{0,2}v L[v]{0,2}[l]{0,2}[v]{0,2}[l]{0,2}l)) how are you doing?  Your SSN is ((Xxx-xx-xxxx))." -c 100` 
- Single repeating symbols using the following syntax
  - `tdg -t "Letters ((L{20})) and Numbers ((X{12}))" -c 100`
- Repeating patterns containing multiple letters or numbers of random length.
  - `[L]{5}` - Will generate 5 random upper-case characters.
  - `[LLX]{24}`  - Will generate 24 repeating letter-letter-number values.
- Variable length data can be generated also
  - `[L]{10,20}` - Will generate a string containing between 10 and 20 characters of random value.
  - `tdg -t "Letters ((L{2,20})) and Numbers ((X{2,12}))" -c 100`

## Profiling results
Profiling results:
- It takes 981ms to generate 1000 strings that match the following large pattern:
  - `"[L]{1}[X]{1}[L]{2}[X]{2}[L]{4}[X]{4}[L]{8}[X]{8}[L]{16}[X]{16}[L]{32}[X]{32}[L]{64}[X]{64}[L]{128}[X]{128}[L]{256}[X]{256}[L]{512}[X]{512}[L]{1024}[X]{1024}"`

##Examples
Executing the following `tdg -t "Letters ((L{2,20})) and Numbers ((X{2,12}))" -c 10` produces the following output:
```
Letters JVMZEDPFXWIMVTDNLRS and Numbers 9185231
Letters VUTSCFFJGTCSPSEDK and Numbers 15824
Letters KXCJZVNDGMOEIQ and Numbers 824220569208
Letters OH and Numbers 431608040
Letters YQHWFYZQRJB and Numbers 989557104
Letters LCWKTQ and Numbers 2648
Letters CHEOOZGXXPWXFNGDKXC and Numbers 90788578
Letters LBXKTHYDBLLNKJAB and Numbers 7682566
Letters KQKRYJBYPJXSQCGXUGU and Numbers 009796
Letters GR and Numbers 31805212297
```

See the unit tests for other examples.