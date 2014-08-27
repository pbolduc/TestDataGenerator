﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace gk.DataGenerator.Generators
{
    public static class AlphaNumericGenerator 
    {
        private static readonly Random Random;

        private const string _AllAllowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!£$%^&*_+;'#,./:@~?";

        private const string _AllLowerLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string _AllUpperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string _AllLetters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string _VowelUpper = "AEIOU";
        private const string _VowelLower = "aeiou";
        private const string _ConsonantLower = "bcdfghjklmnpqrstvwxyz";
        private const string _ConsonantUpper = "BCDFGHJKLMNPQRSTVWXYZ";
        private const string _Numbers0To9Characters = "0123456789";
        private const string _Numbers1To9Characters = "123456789";

        private const string _Placeholder_Start = "<<";
        private const string _Placeholder_End = ">>";

        private const char _Section_Start = '(';
        private const char _Section_End = ')';

        private const char _Set_Start = '[';
        private const char _Set_End = ']';

        private const char _Quantifier_Start = '{';
        private const char _Quantifier_End = '}';

        private const char _Alternation = '|';
        private const char _Escape = '\\';


        static AlphaNumericGenerator()
        {
            Random = new Random(DateTime.Now.Millisecond);
        }

        /// <summary>
        /// Takes in a string that contains 0 or more &lt;&lt;placeholder&gt;&gt; values and replaces the placeholder item with the expression it defines.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public static string GenerateFromTemplate(string template)
        {
            var sb = new StringBuilder();

            int index = 0;
            while (index < template.Length)
            {
                // Find our next placeholder
                int start = FindPositionOfNext(template, index, _Placeholder_Start, _Placeholder_End);
                if (start == -1)
                {
                    sb.Append(template.Substring(index));  //add remaining string.
                    break; // all done!
                }

                sb.Append(template.Substring(index, start - index)); // Append everything up to start as it is.
                start = start + 2; // move past '<<' to start of expression

                int end = FindPositionOfNext(template, start, _Placeholder_End, _Placeholder_Start); // find end of placeholder
                if (end == -1)
                {
                    throw new GenerationException("Unable to find closing placeholder after "+start);
                }

                var pattern = template.Substring(start, end - start); // grab our expression
//                if (pattern.IndexOf(_Alternation) > -1) // check for alternates.
//                {
//                    var exps = pattern.Replace("(","").Replace(")","").Split(_Alternation);
//                    pattern = exps[Random.Next(0, exps.Length)];
//                }

                sb.Append(GenerateFromPattern(pattern)); // generate value from expression
                index = end+2; // update our index.
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern">
        /// The format of the text produced. 15 random characters is the default.
        /// . - An uppercase or lowercase letter or number.
        /// w - Any Letter.  	
        /// L - An uppercase Letter.  	
        /// l - A lowercase letter. 	
        /// V - An uppercase Vowel.
        /// v - A lowercase vowel.
        /// C - An uppercase Consonant. 	
        /// c - A lowercase consonant. 	
        /// n - Any number, 1-9.
        /// d - Any number, 0-9.
        /// -------
        /// Patterns can be produced:
        /// "\.{10}" will produce a random string 10 characters long.
        /// </param>
        /// <returns></returns>
        public static string GenerateFromPattern(string pattern)
        {
            if(pattern == null)
                throw new GenerationException("Argument 'pattern' cannot be null.");

            var sb = new StringBuilder();
            bool isEscaped = false;
            
            int i = 0; 
            while(i < pattern.Length)
            {
                char ch = pattern[i];

                // check for escape chars for next part
                if (ch == _Escape)
                {
                    if (isEscaped)
                    {
                        sb.Append(@"\");
                        isEscaped = false;
                        i++;
                        continue;
                    }
                    isEscaped = true;
                    i++;
                    continue;
                }

                // check are we entering a repeat pattern section that may include a quantifier
                // Format = "(LL){n,m}" = repeat xx pattern 4 times.
                if (!isEscaped && ch == _Section_Start)
                {
                    AppendContentFromSectionExpression(sb, pattern, ref i);
                    continue; // skip to next character - index has already been forwarded to new position
                }

                // check are we entering a set pattern that may include a quantifier
                // Format = "[Vv]{4}" = generate 4 random ordered characters comprising of either V or v characters
                if (!isEscaped && ch == _Set_Start)
                {
                    AppendContentFromSetExpression(sb, pattern, ref i);
                    continue; // skip to next character - index has already been forwarded to new position
                }

                // check are we entering a repeat symbol section
                // Format = "L{4}" = repeat L symbol 4 times.
                bool repeatSymbol = i < pattern.Length -1 && pattern[i + 1] == _Quantifier_Start;
                if (isEscaped && repeatSymbol)
                {
                    AppendRepeatedSymbol(sb, pattern, ref i, isEscaped);
                    isEscaped = false;
                    continue; // skip to next character - index has already been forwarded to new position
                }
                
                AppendCharacterDerivedFromSymbol(sb, ch, isEscaped);
                isEscaped = false;
                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="index"></param>
        /// <param name="toFind"></param>
        /// <param name="notBefore"></param>
        /// <returns></returns>
        private static int FindPositionOfNext(string template, int index, string toFind, string notBefore)
        {
            bool found = false;
            var ndx = index;
            var notBeforeNdx = index;
            while (!found)
            {
                ndx = template.IndexOf(toFind, ndx, StringComparison.Ordinal);
                if (ndx == -1)break;

                notBeforeNdx = template.IndexOf(notBefore, notBeforeNdx, StringComparison.Ordinal);
                // check if escaped
                if (IsEscaped(template, ndx))
                {
                    ndx++; //we found an escaped item, go forward and search again.
                    notBeforeNdx++;
                    continue;
                }

                if (notBeforeNdx > -1 && notBeforeNdx < ndx)
                {
                    BuildErrorSnippet(template, ndx);
                    string msg = @"Found unexpected token '" + notBefore + "' at index '" + notBeforeNdx + "' when seeking '" + toFind + "' starting at index '" + index + "'.";
                    msg = msg + Environment.NewLine + BuildErrorSnippet(template, notBeforeNdx);
                    throw new GenerationException(msg);
                }
                found = true;
            }
            return ndx;
        }

        private static string BuildErrorSnippet(string template, int ndx)
        {
            var context = 50;
            var start = context;
            if (ndx-start < 0) start = ndx -1;// how far back
            var end = start + context;
            if (end > template.Length -1) end = (template.Length - ndx -1); // how far forward

            var line = template.Substring(ndx - start, end).Replace('\n', '_').Replace('\r', '_');
            var indicator = new string('_', start) + "^" + new String('_', end);
            
            return line + Environment.NewLine + indicator;
        }

        private static bool IsEscaped(string template, int ndx)
        {
            int slashes = 0;
            var c = ndx-1;
            while (c >= 0)
            {
                if (template[c] != _Escape) break;

                slashes++;
                c--;
            }
            return (slashes != 0) && slashes%2 != 0;
        }

        /// <summary>
        /// Calculates the content from a repeated symbol when the following form is encountered 's{repeat}' where s is a symbol.
        /// The calculated value is append to sb.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="characters"></param>
        /// <param name="index"></param>
        /// <param name="isEscaped">
        /// True if the the previous character was an escape char.</param>
        /// <returns></returns>
        private static void AppendRepeatedSymbol(StringBuilder sb, string characters, ref int index, bool isEscaped)
        {
            var symbol = characters[index++];
            string repeatExpression = GetSurroundedContent(characters, ref index, _Quantifier_Start, _Quantifier_End);
            int repeat = GetRepeatValueFromRepeatExpression(repeatExpression);

            
                for (int x = 0; x < repeat; x++)
                {
                    AppendCharacterDerivedFromSymbol(sb, symbol, isEscaped);
                }
            
        }

        /// <summary>
        /// Calculates the content from a repeated expression when the following form is enountered '[exp]{repeat}'.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="characters"></param>
        /// <param name="index"></param>
        private static void AppendContentFromSectionExpression(StringBuilder sb, string characters, ref int index)
        {
            var tuple = GetPatternAndRepeatValueFromExpression(characters,_Section_Start, _Section_End, ref index);

            var exp = tuple.Item2;
            if (exp.IndexOf(_Alternation)>-1)
            {
                // alternates in expression 'LL|ll|vv'
                var alternates = exp.Split(_Alternation);
                exp = alternates[Random.Next(0, alternates.Length)];
                sb.Append(GenerateFromPattern(exp));
                return;
            }

            bool isEscaped = false;
            for (int x = 0; x < tuple.Item1; x++)
            {
                for (var curNdx = 0; curNdx < exp.Length; curNdx++ )
                {
                    var chx = exp[curNdx];
                    if (chx == _Escape)
                    {
                        if (isEscaped)
                        {
                            isEscaped = false;
                            sb.Append(chx); // append escaped character.
                            continue;
                        }
                        isEscaped = true;
                        continue;
                    }

                    // check are we entering a set pattern that may include a quantifier
                    // Format = "[Vv]{4}" = generate 4 random ordered characters comprising of either V or v characters
                    if (!isEscaped && chx == _Set_Start)
                    {
                        AppendContentFromSetExpression(sb, exp, ref curNdx);
                        continue; // skip to next character - index has already been forwarded to new position
                    }

                    AppendCharacterDerivedFromSymbol(sb, chx, isEscaped);
                    isEscaped = false;
                }
            }
        }

        /// <summary>
        /// Calculates the content from a set expression when the following form is enountered '[exp]{repeat}'.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="characters"></param>
        /// <param name="index"></param>
        private static void AppendContentFromSetExpression(StringBuilder sb, string characters, ref int index)
        {
            var tuple = GetPatternAndRepeatValueFromExpression(characters, _Set_Start, _Set_End, ref index);
            var possibles = tuple.Item2.ToCharArray();

            if (tuple.Item2.Contains("-")) // Ranged - [0-7] or [a-z] or [1-9A-Za-z] for fun.
            {
                var tmp = "";
                MatchCollection ranges = new Regex(@"\D-\D|\d+\.?\d*-\d+\.?\d*").Matches(tuple.Item2);
                for (int i = 0; i < tuple.Item1; i++)
                {
                    var range = ranges[Random.Next(0, ranges.Count)];
                    sb.Append(GetRandomCharacterFromRange(range.ToString()));
                }
            }
            else
            {
                for (int i = 0; i < tuple.Item1; i++)
                {
                    sb.Append(possibles[Random.Next(0, possibles.Length)]);
                }
            }
        }

        /// <summary>
        /// Recieves a "A-Z" type string and returns the appropriate list of characters.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        private static string GetRandomCharacterFromRange(string range)
        {
            string ret = "";
            string possibles = "";
            var items = range.Split('-');

            var start = _AllLowerLetters.IndexOf(items[0].ToString(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
            if (start > -1)
            {
                var end = _AllLowerLetters.IndexOf(items[1].ToString(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
                possibles = _AllLowerLetters.Substring(start, end - start+1);
                ret = possibles[Random.Next(0, possibles.Length)].ToString();
                return ret;
            }

            start = _AllUpperLetters.IndexOf(items[0].ToString(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
            if (start > -1)
            {
                var end = _AllUpperLetters.IndexOf(items[1].ToString(CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
                possibles = _AllUpperLetters.Substring(start, end - start+1);
                ret = possibles[Random.Next(0, possibles.Length)].ToString();
                return ret;
            }
            
            // NUMERIC RANGES
            if (int.TryParse(items[0], out start))
            {
                var upper = -1;
                if(int.TryParse(items[1], out upper))
                    ret = Random.Next(start, upper+1).ToString(CultureInfo.InvariantCulture);
                return ret;
            }

            double min = 0d;
            if (double.TryParse(items[0], out min))
            {
                double max = 0d;
                if (double.TryParse(items[1], out max))
                {
                    int scale = 0;
                    if (items[0].Contains("."))
                    {
                        scale = items[0].Split('.')[1].Length;
                    }
                    var t = Random.NextDouble();
                    min = min + (t * (max - min));
                    ret = min.ToString(generateFloatingFormatWithScale(scale), CultureInfo.InvariantCulture);
                }
                return ret;
            }
            
            return ret;
        }

        private static string generateFloatingFormatWithScale(int scale)
        {
            var t = "#.";
            for (int i = 0; i < scale; i++)
            {
                t += "#";
            }
            return t;
        }


        /// <summary>
        /// Returns a tuple containing an integer representing the number of repeats and a string representing the pattern.
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static Tuple<int,string> GetPatternAndRepeatValueFromExpression(string characters, char startChar, char endChar, ref int index)
        {
            string pattern = GetSurroundedContent(characters, ref index, startChar, endChar);
            string repeatExpression = GetSurroundedContent(characters, ref index, _Quantifier_Start, _Quantifier_End);

            int repeat;
            repeat = GetRepeatValueFromRepeatExpression(repeatExpression);

            return new Tuple<int, string>(repeat, pattern);
        }

        /// <summary>
        /// Dervives the correct repeat value from the provided expression.
        /// </summary>
        /// <param name="repeatExpression">String in the form of '{n}' or '{n,m}' where n and m are integers</param>
        /// <returns></returns>
        private static int GetRepeatValueFromRepeatExpression(string repeatExpression)
        {
            if (string.IsNullOrWhiteSpace(repeatExpression)) return 1; 

            int repeat;
            if (repeatExpression.Contains(","))
            {
                // {min,max} has been provided - parse and get value.
                var vals = repeatExpression.Split(',');
                int min = -1, max = -1;

                if (vals.Length < 2 || !int.TryParse(vals[0], out min) || !int.TryParse(vals[1], out max) || min > max || min < 0)
                    throw new GenerationException("Invalid repeat section, random length parameters must be in the format {min,max} where min and max are greater than zero and min is less than max.");

                repeat = Random.Next(min, max + 1);
            }
            else if (!int.TryParse(repeatExpression, out repeat)) repeat = -1;

            if (repeat < 0)
                throw new GenerationException("Invalid repeat section, repeat value must not be less than zero.");
            return repeat;
        }


        private static string GetSurroundedContent(string characters, ref int index, char sectionStartChar, char sectionEndChar)
        {
            if (index == characters.Length)
                return ""; // throw new GenerationException("Expected '" + sectionStartChar + "' at " + index + " but reached end of pattern instead.");
            if (characters[index].Equals(sectionStartChar) == false)
                return ""; // return blank string if expected character is not found.

            int patternStart = index + 1;

            var sectionDepth = 1; // start off inside current section
            var patternEnd = patternStart;
            while (patternEnd < characters.Length)
            {
                if (characters[patternEnd] == sectionStartChar) sectionDepth++;

                if (characters[patternEnd] == sectionEndChar)
                {
                    sectionDepth--;
                    if (sectionDepth == 0) break;
                }
                patternEnd++;
            }
            if (sectionDepth > 0) // make sure we found closing char
                throw new GenerationException("Expected '" + sectionEndChar + "' but it was not found.");

            int patternLength = patternEnd - patternStart;
            if(patternLength <= 0)
                throw new GenerationException("Expected '"+ sectionEndChar +"' but it was not found.");

            index = index + patternLength + 2; // update index position.
            return characters.Substring(patternStart, patternLength);
        }

        private static void AppendCharacterDerivedFromSymbol(StringBuilder sb, char symbol, bool isEscaped)
        {
            if (!isEscaped)
            {
                sb.Append(symbol); // not a symbol - append as is.
                return;
            }

            switch (symbol)
            {
                case '.':
                    AppendRandomCharacterFromString(sb, _AllAllowedCharacters);
                    break;
                case 'w':
                    AppendRandomCharacterFromString(sb, _AllLetters);
                    break;
                case 'L':
                    AppendRandomCharacterFromString(sb, _AllUpperLetters);
                    break;
                case 'l':
                    AppendRandomCharacterFromString(sb, _AllLowerLetters);
                    break;
                case 'V':
                    AppendRandomCharacterFromString(sb, _VowelUpper);
                    break;
                case 'v':
                    AppendRandomCharacterFromString(sb, _VowelLower);
                    break;
                case 'C':
                    AppendRandomCharacterFromString(sb, _ConsonantUpper);
                    break;
                case 'c':
                    AppendRandomCharacterFromString(sb, _ConsonantLower);
                    break;
                case 'D':
                    AppendRandomCharacterFromString(sb, _Numbers0To9Characters);
                    break;
                case 'd':
                    AppendRandomCharacterFromString(sb, _Numbers1To9Characters);
                    break;
                case 'n':
                    sb.Append(Environment.NewLine);
                    break;
                case 't':
                    sb.Append("\t");
                    break;
                default:
                    // Just append the character as it is not a symbol.
                    sb.Append(symbol);
                    break;
            }
        }

        private static void AppendRandomCharacterFromString(StringBuilder sb, string allowedCharacters)
        {
            sb.Append(allowedCharacters[Random.Next(allowedCharacters.Length)]);
        }
    }
}
