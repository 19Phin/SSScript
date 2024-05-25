using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

public class Lexer {
    private string sourceCode;
    private int position = 0;
    private int length;

    public Lexer(string source) {
        sourceCode = source;
        length = source.Length;
    }

    public List<Token> Tokenize() {
        List<Token> tokens = new List<Token>();

        int parDepth = 0;
        int braceDepth = 0;

        while (position < length) {
            char current = Peek();

            string value;
            switch (current) {
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    ReadChar();
                    break;
                
                
                case '{':
                    tokens.Add(new Token(TokenType.OpenBrace, braceDepth.ToString(), position));
                    braceDepth ++;
                    ReadChar();
                    break;

                case '}':
                    braceDepth --;
                    tokens.Add(new Token(TokenType.CloseBrace, braceDepth.ToString(), position));
                    ReadChar();
                    break;

                case '(':
                    tokens.Add(new Token(TokenType.OpenParenthesis, parDepth.ToString(), position));
                    parDepth ++;
                    ReadChar();
                    break;

                case ')':
                    parDepth --;
                    tokens.Add(new Token(TokenType.CloseParenthesis, parDepth.ToString(), position));
                    ReadChar();
                    break;

                case '.':
                    tokens.Add(new Token(TokenType.Dot, ReadChar().ToString(), position));
                    break;

                case ';':
                    tokens.Add(new Token(TokenType.Semicolon, ReadChar().ToString(), position));
                    break;

                case '!':
                    tokens.Add(new Token(TokenType.Not, ReadChar().ToString(), position));
                    break;

                case ',':
                    tokens.Add(new Token(TokenType.Comma, ReadChar().ToString(), position));
                    break;

                case '*':
                    value = ReadChar().ToString();
                    if (Peek() == '*') {
                        value += ReadChar();
                        tokens.Add(new Token(TokenType.Power, value, position));
                    } else {
                        tokens.Add(new Token(TokenType.Multiply, value, position));
                    }
                    break;

                case '/':
                    tokens.Add(new Token(TokenType.Divide, ReadChar().ToString(), position));
                    break;

                case '%':
                    tokens.Add(new Token(TokenType.Mod, ReadChar().ToString(), position));
                    break;

                case '+':
                    tokens.Add(new Token(TokenType.Plus, ReadChar().ToString(), position));
                    break;

                case '-':
                    tokens.Add(new Token(TokenType.Minus, ReadChar().ToString(), position));
                    break;

                case '>':
                    value = ReadChar().ToString();
                    if (Peek() == '=') {
                        value += ReadChar();
                        tokens.Add(new Token(TokenType.GreaterOrEqual, value, position));
                    } else {
                        tokens.Add(new Token(TokenType.Greater, value, position));
                    }
                    break;

                case '<':
                    value = ReadChar().ToString();
                    if (Peek() == '=') {
                        value += ReadChar();
                        tokens.Add(new Token(TokenType.LessOrEqual, value, position));
                    } else {
                        tokens.Add(new Token(TokenType.Less, value, position));
                    }
                    break;

                case '=':
                    value = ReadChar().ToString();
                    if (Peek() == '=') {
                        value += ReadChar();
                        tokens.Add(new Token(TokenType.EqualEqual, value, position));
                    } else {
                        tokens.Add(new Token(TokenType.Equal, value, position));
                    }
                    break;

                case '&':
                    if (Peek(1) == '&') {
                        value = ReadChar().ToString();
                        value += ReadChar();
                        tokens.Add(new Token(TokenType.And, value, position));
                    } else throw new SyntaxErrorException("Unknown charachter '"+ ReadChar() +"'");
                    break;

                case '|':
                    if (Peek(1) == '|') {
                        value = ReadChar().ToString();
                        value += ReadChar();
                        tokens.Add(new Token(TokenType.OrOr, value, position));
                    }
                    else throw new SyntaxErrorException("Unknown charachter '"+ ReadChar() +"'");
                    break;

                default:
                    if (char.IsDigit(Peek())) {
                        tokens.Add(TokenizeNumber());
                    }else if (char.IsWhiteSpace(current)) {
                        tokens.Add(TokenizeWhitespace());
                    } else if (IsLetterOrUnderscore(current)) {
                        tokens.Add(TokenizeIdentifierOrKeyword());
                    } else {
                        throw new SyntaxErrorException("Unknown charachter '"+ ReadChar() +"'");
                    }
                    break;
            }
        }
        tokens.Add(new Token(TokenType.EOF, "end", position));

        return tokens;
    }

    private char Peek(int forward = 0) => position < length ? sourceCode[position + forward] : '\0';

    private char ReadChar() => sourceCode[position++];

    private bool IsLetterOrUnderscore(char c) => char.IsLetter(c) || c == '_';

    private Token TokenizeNumber() {
        StringBuilder sb = new StringBuilder();
        while (char.IsDigit(Peek()) || Peek() == '.' || Peek() == 'e') {
            sb.Append(ReadChar());
        }
        return new Token(TokenType.Number, sb.ToString(), position);
    }

    private Token TokenizeWhitespace() {
        while (char.IsWhiteSpace(Peek())) {
            ReadChar();
        }
        return new Token(TokenType.Whitespace, " ", position);
    }

    private Token TokenizeIdentifierOrKeyword() {
        StringBuilder sb = new StringBuilder();
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_') {
            sb.Append(ReadChar());
        }
        if (Peek() == '.') {
            return new Token(TokenType.MethodCall, sb.ToString(), position);
        }
        string identifier = sb.ToString();

        TokenType type = identifier switch {
            "int" => TokenType.VarDef,
            "bool" => TokenType.VarDef,
            "float" => TokenType.VarDef,
            "def" => TokenType.Def,
            "if" => TokenType.If,
            "else" => TokenType.Else,
            "return" => TokenType.Return,
            "Vector2" => TokenType.VarDef,
            "Vector3" => TokenType.VarDef,
            "public" => TokenType.Public,
            "new" => TokenType.New,
            _ => TokenType.Identifier
        };

        return new Token(type, identifier, position);
    }
}

public enum TokenType {
    Identifier, Number, Whitespace, MethodCall,
    
    VarDef, Def, If, Else, Return, Public, New,
    
    Power, Multiply, Divide, Mod, Plus, Minus,
    Greater, GreaterOrEqual, Less, LessOrEqual, EqualEqual, And, OrOr, Not,
    
    Dot, Comma, OpenBrace, CloseBrace, OpenParenthesis, CloseParenthesis, Equal, Semicolon,

    EOF
}

public class Token {
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int Position { get; set; }

    public Token(TokenType type, string value, int position) {
        Type = type;
        Value = value;
        Position = position;
    }
}