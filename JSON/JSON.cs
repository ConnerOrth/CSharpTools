﻿/* ======================================================================= *
 * CSharpTools are meant to be useful and reusable
 *
 * JSON (JavaScript Object Notation)
 * License: MIT (Copyright Charlie Volpe) --- See LICENSE file.
 * 
 * Author: Charlie Volpe
 * Started: 2019-12-16
 * Version: 1.0.0
 * 
 * Description:
 * An implementation of the JSON format for C# & .Net that is easy to use
 * and that follows the standard. The goal is to make it work well for users
 * a bit easier than the data contract serialization classes.
 *
 * On JSONObject and JSONArray:
 * Any validation of the types that "should" or "shouldn't" match
 * inside of a JSONObject or JSONArray is not handled. The JSONObject or
 * JSONArray will be able to contain any value of a known type. It is
 * up to the data provider / receiver to validate the data.
 * 
 * JSON Standard: ECMA-404
 * https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
 * ======================================================================= */

using System;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

// Examples in summaries below have white-space added for readability, but are
// placed on a single line as to not make the code too spaced out.

namespace CSharpTools
{
    /// <summary>
    /// JSON class
    /// 
    /// Encoding expected is UTF-8 based on the JSON standard ECMA-404 (see link above)
    ///
    /// Tokens: '{', '}', '[', ']', ',', '"', ''', ':', '(-)[0-9]*.[0-9]*e(+/-)[0-9]*', 'true', 'false', 'null'
    /// 
    /// Example:
    /// A full json can include any number of JSONElements inside the root object:
    /// --> { "users": [ { "name": "Smith", "level": "Admin" }, { "name": "Doe", "level": "Member" } ] }
    /// but should at least have a root object:
    /// --> { }
    /// 
    /// </summary>
    public class JSON
    {
        private JSONElement _root;

        /// <summary>
        /// Set _root by string key if _root is of Type JSONObject
        /// </summary>
        /// <param name="key">String Key</param>
        /// <exception cref="ArgumentException">Exception thrown if _root is null or invalid type</exception>
        public JSONElement this[string key]
        {
            get
            {
                if (_root == null || _root.GetDataType() != typeof(JSONObject))
                {
                    throw new ArgumentException("JSON root is not of type JSONObject but is trying to be accessed by string key.");
                }
                
                JSONObject obj = _root;
                return obj[key];
            }
            set
            {
                if (_root == null || _root.GetDataType() != typeof(JSONObject))
                {
                    throw new ArgumentException("JSON root is not of type JSONObject but is trying to be accessed by string key.");
                }

                JSONObject obj = _root;
                obj[key] = value;
            }
        }

        /// <summary>
        /// Set _root by index if _root is of Type JSONArray
        /// </summary>
        /// <param name="i">Index</param>
        /// <exception cref="ArgumentException">Exception thrown if _root is null or invalid type</exception>
        public JSONElement this[int i]
        {
            get
            {
                if (_root == null || _root.GetDataType() != typeof(JSONArray))
                {
                    throw new ArgumentException("JSON root is not of type JSONArray but is trying to be accessed by index.");
                }

                JSONArray arr = _root;
                return arr[i];
            }
            set
            {
                if (_root == null || _root.GetDataType() != typeof(JSONArray))
                {
                    throw new ArgumentException("JSON root is not of type JSONArray but is trying to be accessed by index.");
                }

                JSONArray arr = _root;
                arr[i] = value;
            }
        }
        
        public JSON()
        {
            _root = new JSONObject();
        }

        public JSON(string content)
        {
            Deserialize(content);
        }

        ~JSON()
        {
            _root = null;
        }

        /// <summary>
        /// Deserialize the content into a full JSON
        /// </summary>
        /// <param name="content">String data to deserialize</param>
        /// <exception cref="JsonException">Exception thrown when invalid json is found</exception>
        public void Deserialize(string content)
        {
            // Remove all whitespace from the content leaving just the data
            string data = Regex.Replace(content, @"\s+", "");
            
            // Make sure that data starts with JSONObject
            if (!(data[0] == '{' || data[0] == '['))
            {
                throw new JsonException("content is not of type json or doesn't start with '{' or '['");
            }

            _root = JSONElement.Deserialize(data);
        }

        /// <summary>
        /// Serializes the full JSON into a data string
        /// </summary>
        /// <returns>String data of _root JSONElement</returns>
        public string Serialize()
        {
            return _root.Serialize();
        }
    }

    /// <summary>
    /// JSON Token Class
    ///
    /// Tokens are used in parsing the data. The parser uses the
    /// token type to figure out how to parse the data and it
    /// parses the value based on that token type.
    /// 
    /// </summary>
    public class JSONToken
    {
        public enum ETokenType
        {
            OpenCurly = 0,
            CloseCurly,
            OpenBracket,
            CloseBracket,
            Colon,
            Comma,
            String,
            Number,
            Boolean,
            Null,
            Invalid = -1
        };

        private ETokenType _token;
        private string _value;

        public JSONToken(ETokenType token, string value)
        {
            _token = token;
            _value = value;
        }

        ~JSONToken()
        {
            _token = ETokenType.Invalid;
            _value = null;
        }

        public ETokenType GetTokenType()
        {
            return _token;
        }

        public string GetTokenValue()
        {
            return _value;
        }
    }
    
    /// <summary>
    /// JSON Element class
    ///
    /// JSONElement is the class that holds this all together. It gets and sets
    /// implicit casting to JSONObject, JSONArray, double, string and bool. They
    /// are separated in the serialization with ','.
    ///
    /// ',' U+002C comma --- separates elements
    /// </summary>
    public class JSONElement : object
    {
        private Type _type;
        private object _data;
        
        private JSONElement(object data)
        {
            _type = data.GetType();
            _data = data;
        }

        ~JSONElement()
        {
            _type = null;
            _data = null;
        }

        /// <summary>
        /// Get the Type of the stored data
        /// </summary>
        /// <returns>Type</returns>
        public Type GetDataType()
        {
            return _type;
        }

        /// <summary>
        /// Cast JSONElement to JSONArray
        /// </summary>
        /// <param name="element">JSONElement to cast</param>
        /// <returns>JSONArray or default</returns>
        public static implicit operator JSONArray(JSONElement element)
        {
            if (element != null && element._type == typeof(JSONArray))
            {
                return (JSONArray)element._data;
            }

            return default;
        }

        /// <summary>
        /// Cast JSONArray to JSONElement
        /// </summary>
        /// <param name="arr">JSONArray to cast</param>
        /// <returns>new JSONElement</returns>
        public static implicit operator JSONElement(JSONArray arr)
        {
            return new JSONElement(arr);
        }

        /// <summary>
        /// Cast JSONElement to JSONObject
        /// </summary>
        /// <param name="element">JSONElement to cast</param>
        /// <returns>JSONObject or default</returns>
        public static implicit operator JSONObject(JSONElement element)
        {
            if (element != null && element._type == typeof(JSONObject))
            {
                return (JSONObject) element._data;
            }

            return default;
        }

        /// <summary>
        /// Cast JSONObject to JSONElement
        /// </summary>
        /// <param name="obj">JSONObject to cast</param>
        /// <returns>new JSONElement</returns>
        public static implicit operator JSONElement(JSONObject obj)
        {
            return new JSONElement(obj);
        }

        /// <summary>
        /// Cast JSONElement to double
        /// </summary>
        /// <param name="element">JSONElement to cast</param>
        /// <returns>double or default</returns>
        public static implicit operator double(JSONElement element)
        {
            if (element == null)
            {
                return default;
            }
            
            if(element._type == typeof(double))
            {
                return (double) element._data;
            }

            throw new ArgumentException("element is not a double", nameof(element));
        }

        /// <summary>
        /// Cast double to JSONElement
        /// </summary>
        /// <param name="dbl">double to cast</param>
        /// <returns>new JSONElement</returns>
        public static implicit operator JSONElement(double dbl)
        {
            return new JSONElement(dbl);
        }

        /// <summary>
        /// Cast JSONElement to bool
        /// </summary>
        /// <param name="element">JSONElement to cast</param>
        /// <returns>bool or default</returns>
        public static implicit operator bool(JSONElement element)
        {
            if (element == null)
            {
                return default;
            }

            if (element._type == typeof(bool))
            {
                return (bool) element._data;
            }
            
            throw new ArgumentException("element is not a bool", nameof(element));
        }

        /// <summary>
        /// Cast bool to JSONElement
        /// </summary>
        /// <param name="bln">bool to cast</param>
        /// <returns>new JSONElement</returns>
        public static implicit operator JSONElement(bool bln)
        {
            return new JSONElement(bln);
        }

        /// <summary>
        /// Cast JSONElement to string
        /// </summary>
        /// <param name="element">JSONElement to cast</param>
        /// <returns>string value or default</returns>
        public static implicit operator string(JSONElement element)
        {
            if (element != null && element._type == typeof(string))
            {
                return (string) element._data;
            }

            return default;
        }
        
        /// <summary>
        /// Cast string to JSONElement
        /// </summary>
        /// <param name="str">string to cast</param>
        /// <returns>new JSONElement</returns>
        public static implicit operator JSONElement(string str)
        {
            return new JSONElement(str);
        }

        /// <summary>
        /// Serialize encodes the _root object recursively with EncodeElement
        /// and returns the resulting string data.
        /// </summary>
        /// <returns>Serialized string data</returns>
        public string Serialize()
        {
            string encoded = "";
            
            EncodeElement(ref encoded, this);
            
            return encoded;
        }
        
        /// <summary>
        /// EncodeElement goes through the element recursively and
        /// creates the encoded string data as it goes. JSONObjects and
        /// JSONArrays keep drilling down until it reaches a base
        /// JSONElement. When recursion completes, the final
        /// string is stored in the referenced string provided.
        /// </summary>
        /// <param name="encoded">String data where the encoded data is stored</param>
        /// <param name="element">The current JSONElement to be encoded</param>
        /// <exception cref="DataException">Thrown if the data type is invalid</exception>
        private void EncodeElement(ref string encoded, JSONElement element)
        {
            if (element == null)
            {
                encoded += "null";
            }
            else if (element.GetDataType() == typeof(JSONObject))
            {
                encoded += "{";
                JSONObject obj = element;
                
                foreach (KeyValuePair<string, JSONElement> item in obj.GetData())
                {
                    encoded += $"\"{item.Key}\":";
                    EncodeElement(ref encoded, item.Value);
                    encoded += ",";
                }

                // Remove the unneeded trailing comma before capping the JSONObject
                encoded = encoded.TrimEnd(',');
                encoded += "}";
            }
            else if (element.GetDataType() == typeof(JSONArray))
            {
                encoded += "[";
                JSONArray arr = element;

                foreach (JSONElement item in arr.GetData())
                {
                    EncodeElement(ref encoded, item);
                    encoded += ",";
                }

                // Remove the unneeded trailing comma before capping the JSONArray
                encoded = encoded.TrimEnd(',');
                encoded += "]";
            }
            else if (element.GetDataType() == typeof(string))
            {
                encoded += $"\"{(string)element}\"";
            }
            else if (element.GetDataType() == typeof(double))
            {
                encoded += ((double) element).ToString(CultureInfo.InvariantCulture);
            }
            else if (element.GetDataType() == typeof(bool))
            {
                if ((bool) element)
                {
                    encoded += "true";
                }
                else
                {
                    encoded += "false";
                }
            }
            else
            {
                throw new DataException("Invalid data type when encoding JSON.");
            }
        }
        
        /// <summary>
        /// Deserialize takes in string data, tokenizes it, parses the tokens
        /// and sets the root object of JSON to the main JSONObject of the data.
        /// </summary>
        /// <param name="content">String data to deserialize</param>
        /// <returns>JSONElement that was parsed</returns>
        public static JSONElement Deserialize(string content)
        {
            // Remove all whitespace from the content leaving just the data
            string data = Regex.Replace(content, @"\s+", "");
            
            // Tokenize the data
            List<JSONToken> tokens = GetTokens(data);

            // Parse the data
            int index = 0;
            return ParseToken(ref index, tokens);
        }

        /// <summary>
        /// ParseToken goes through the token list recursively and
        /// creates the json data object as it goes. JSONObjects and
        /// JSONArrays keep drilling down until it reaches a base
        /// JSONElement. When recursion completes, store the final
        /// returned element into the _root object of JSON.
        /// </summary>
        /// <param name="index">Position in tokens list, passed by reference</param>
        /// <param name="tokens">The tokens to be parsed</param>
        /// <returns>JSONElement parsed</returns>
        /// <exception cref="JsonException">Exception thrown if invalid json data found</exception>
        /// <exception cref="ArgumentOutOfRangeException">Exception thrown if invalid token type</exception>
        private static JSONElement ParseToken(ref int index, List<JSONToken> tokens)
        {
            JSONElement element;
            
            switch (tokens[index].GetTokenType())
            {
                case JSONToken.ETokenType.OpenCurly:
                {
                    JSONObject obj = new JSONObject();
                    index++;

                    while (tokens[index].GetTokenType() != JSONToken.ETokenType.CloseCurly)
                    {
                        if (tokens[index].GetTokenType() == JSONToken.ETokenType.Comma)
                        {
                            index++;
                        }
                        
                        if (tokens[index].GetTokenType() != JSONToken.ETokenType.String ||
                            tokens[index + 1].GetTokenType() != JSONToken.ETokenType.Colon)
                        {
                            Console.WriteLine("Token: " + tokens[index].GetTokenValue());
                            Console.WriteLine("Next Token:" + tokens[index + 1].GetTokenValue());
                            throw new JsonException("Parse issue. Expected string then colon.");
                        }

                        string val = tokens[index].GetTokenValue();
                        string key = val.Substring(1, val.Length - 2);
                        
                        index += 2;
                        
                        obj[key] = ParseToken(ref index, tokens);
                    }
                    
                    element = obj;
                } break;
                case JSONToken.ETokenType.OpenBracket:
                {
                    JSONArray arr = new JSONArray();
                    index++;

                    int count = 0;
                    while (tokens[index].GetTokenType() != JSONToken.ETokenType.CloseBracket)
                    {
                        arr[count] = ParseToken(ref index, tokens);
                        
                        if (tokens[index + 1].GetTokenType() == JSONToken.ETokenType.Comma)
                        {
                            index++;
                        }

                        index++;
                        count++;
                    }

                    element = arr;
                } break;
                case JSONToken.ETokenType.String:
                {
                    string val = tokens[index].GetTokenValue();
                    string str = val.Substring(1, val.Length - 2);
                    
                    index++;
                    
                    element = str;
                } break;
                case JSONToken.ETokenType.Number:
                {
                    string val = tokens[index].GetTokenValue();
                    double dbl = double.Parse(val);

                    index++;

                    element = dbl;
                } break;
                case JSONToken.ETokenType.Boolean:
                {
                    string val = tokens[index].GetTokenValue();
                    bool b = bool.Parse(val);

                    index++;

                    element = b;
                } break;
                case JSONToken.ETokenType.Null:
                {
                    index++;

                    element = null;
                } break;
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            
            return element;
        }
        
        /// <summary>
        /// GetTokens walks through the characters in the data and creates tokens.
        /// </summary>
        /// <param name="data">String data to parse</param>
        /// <returns>A list of the tokens found in the string data</returns>
        /// <exception cref="JsonException">Exception thrown if invalid json data found</exception>
        private static List<JSONToken> GetTokens(string data)
        {
            List<JSONToken> tokens = new List<JSONToken>();
            
            // Cycles through the data and removes the characters it has tokenized as it goes
            while (data.Length > 0 && data[0] != '\0')
            {
                switch (data[0])
                {
                    case '{': // '\u007B'
                    {
                        tokens.Add(new JSONToken(JSONToken.ETokenType.OpenCurly, data[0].ToString()));
                        data = data.Remove(0, 1);
                    } break;
                    case '[': // '\u005B'
                    {
                        tokens.Add(new JSONToken(JSONToken.ETokenType.OpenBracket, data[0].ToString()));
                        data = data.Remove(0, 1);
                    } break;
                    case ':': // '\u003A'
                    {
                        tokens.Add(new JSONToken(JSONToken.ETokenType.Colon, data[0].ToString()));
                        data = data.Remove(0, 1);
                    } break;
                    case ',': // '\u002C'
                    {
                        tokens.Add(new JSONToken(JSONToken.ETokenType.Comma, data[0].ToString()));
                        data = data.Remove(0, 1);
                    } break;
                    case '}': // '\u007D'
                    {
                        tokens.Add(new JSONToken(JSONToken.ETokenType.CloseCurly, data[0].ToString()));
                        data = data.Remove(0, 1);
                    } break;
                    case ']': // '\u005D'
                    {
                        tokens.Add(new JSONToken(JSONToken.ETokenType.CloseBracket, data[0].ToString()));
                        data = data.Remove(0, 1);
                    } break;
                    case 't': // '\u0074', '\u0072', '\u0075', '\u0065'
                    {
                        if (data[1] == 'r' && data[2] == 'u' && data[3] == 'e')
                        {
                            tokens.Add(new JSONToken(JSONToken.ETokenType.Boolean, "true"));
                            data = data.Remove(0, 4);
                        }
                        else
                            throw new JsonException("Invalid token starting with 't'! 'true' expected..");
                    } break;
                    case 'n': // '\u006E', '\u0075', '\u006C', '\u006C'
                    {
                        if (data[1] == 'u' && data[2] == 'l' && data[3] == 'l')
                        {
                            tokens.Add(new JSONToken(JSONToken.ETokenType.Null, "null"));
                            data = data.Remove(0, 4);
                        }
                        else
                            throw new JsonException("Invalid token starting with 'n'! 'null' expected..");
                    } break;
                    case 'f': // '\u0066', '\u0061', '\u006C', '\u0073', '\u0065'
                    {
                        if (data[1] == 'a' && data[2] == 'l' && data[3] == 's' && data[4] == 'e')
                        {
                            tokens.Add(new JSONToken(JSONToken.ETokenType.Boolean, "false"));
                            data = data.Remove(0, 5);
                        }
                        else
                            throw new JsonException("Invalid token starting with 'f'! 'false' expected..");
                    } break;
                    case '"': // '\u0022'
                    {
                        int[] indices =
                        {
                            data.IndexOf("\":", StringComparison.Ordinal),
                            data.IndexOf("\",", StringComparison.Ordinal),
                            data.IndexOf("\"}", StringComparison.Ordinal),
                            data.IndexOf("\"]", StringComparison.Ordinal)
                        };
                        
                        if (indices[0] < 0 && indices[1] < 0 && indices[2] < 0 && indices[3] < 0)
                        {
                            throw new JsonException("Didn't find expected end of string!");
                        }

                        int index = Int32.MaxValue;

                        for (int i = 0; i < indices.Length; i++)
                        {
                            if (indices[i] >= 0 && indices[i] < index)
                            {
                                index = indices[i];
                            }
                        }
                        
                        string str = data.Substring(0, index + 1);
                        tokens.Add(new JSONToken(JSONToken.ETokenType.String, str));
                        data = data.Remove(0, str.Length);
                    } break;
                    case '-': // '\u002D'
                    case '0': // '\u0030'
                    case '1': // '\u0031'
                    case '2': // '\u0032'
                    case '3': // '\u0033'
                    case '4': // '\u0034'
                    case '5': // '\u0035'
                    case '6': // '\u0036'
                    case '7': // '\u0037'
                    case '8': // '\u0038'
                    case '9': // '\u0039'
                    {
                        int count = 0;
                        char c = data[count];
                        while ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                        {
                            count++;
                            c = data[count];
                        }

                        string str = data.Substring(0, count);
                        try
                        {
                            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                            double.Parse(str); // Only done to test if valid double
                        }
                        catch
                        {
                            throw new JsonException($"Invalid token when expecting number! Look for \"{str}\"..");
                        }
                        tokens.Add(new JSONToken(JSONToken.ETokenType.Number, str));
                        data = data.Remove(0, str.Length);
                    } break;
                    case '+': // '\u002B'
                    case '.': // '\u002E'
                    case 'e': // '\u0065'
                    case 'E': // '\u0045'
                    {
                        throw new JsonException($"Invalid token: '{data[0]}'! Numbers should start with a digit or '-'.");
                    }
                    case '\'': // '\u0027'
                    {
                        throw new JsonException($"Invalid token: '{data[0]}'! Strings should start with a '\"'.");
                    }
                }
            }

            return tokens;
        }
    }

    /// <summary>
    /// JSON Object class
    ///
    /// JSONObject is a Dictionary, which is serialized with '{' and '}'.
    /// The elements inside are keyed with a string "key". The value
    /// can be anything that a JSONElement can become.
    ///
    /// Example:
    /// A full object can look something like:
    /// --> { "name": "Smith" }
    /// or:
    /// --> { "user": { "name": "Smith", "level": "Admin" } }
    /// etc.
    /// 
    /// '{' U+007B left curly bracket --- begins the object
    /// ':' U+003A colon --- separates a key and a value
    /// '}' U+007D right curly bracket --- ends the object
    /// </summary>
    public class JSONObject
    {
        private Dictionary<string, JSONElement> _data;
        
        public JSONElement this[string key]
        {
            get => _data[key];
            set => _data[key] = value;
        }
        
        public JSONObject()
        {
            _data = new Dictionary<string, JSONElement>();
        }

        ~JSONObject()
        {
            _data.Clear();
            _data.TrimExcess();
            _data = null;
        }

        public Dictionary<string, JSONElement> GetData()
        {
            return _data;
        }
    }

    /// <summary>
    /// JSON Array class
    ///
    /// JSONArray is a List, which is serialized with '[' and ']'. The
    /// value can be anything that a JSONElement can become.
    ///
    /// Example:
    /// A full array can look something like:
    /// --> [ 0.0, 1.0, 1.1 ]
    /// or:
    /// --> [ { "name": "Smith" }, { "name": "Doe" } ]
    /// etc.
    /// 
    /// '[' U+005B left square bracket --- begins the array
    /// ']' U+005D right square bracket --- ends the array
    /// </summary>
    public class JSONArray
    {
        private List<JSONElement> _data;

        public JSONElement this[int i]
        {
            get => _data[i];
            set
            {
                while (_data.Count <= i)
                {
                    _data.Add(null);
                }
                _data[i] = value;
            }
        }

        public JSONArray()
        {
            _data = new List<JSONElement>();
        }

        ~JSONArray()
        {
            _data.Clear();
            _data.TrimExcess();
            _data = null;
        }

        public List<JSONElement> GetData()
        {
            return _data;
        }
    }
}