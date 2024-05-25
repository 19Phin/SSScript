using System.Data;

public class Parser {
    private readonly List<Token> tokens;
    private int currentPosition = 0;

    public Parser(List<Token> tokens) {
        this.tokens = tokens;
    }
    
    public BaseNode Parse() {
        List<Node> nodes = new List<Node>();
        while (!IsAtEnd()) {
            if (Match(TokenType.Def)) {
                nodes.Add(ParseMethod());
            } else if (Match(TokenType.VarDef)) {
                nodes.Add(ParseGlobalDefinition());
            } else {
                throw new SyntaxErrorException("What is '" + Peek().Value + "'? at " + Peek().Position);
            }
        }
        return new BaseNode(nodes);
    }

    private Node ParsePublic () {
        var type = Consume(TokenType.VarDef, "Expect type");
        var identifier = Consume(TokenType.Identifier, "Expect variable name");
        if (Match(TokenType.Equal)) {
            throw new Exception("Public variable cannot be initialized");
        } else {
            Consume(TokenType.Semicolon, "Expect ';'");
            return new VarDeclarationNode(type.Value, identifier.Value, true);
        }
    }

    private Node ParseMethod() {
        var methodNameToken = Consume(TokenType.Identifier, "Expect method name");
        Consume(TokenType.OpenParenthesis, "Expect '(' after method name");
        var parentheses = ParseNewArguments();
        Consume(TokenType.OpenBrace, "Expect '{' to begin method");
        var content = ParseBody();
        return new MethodNode(methodNameToken.Value, parentheses, content);
    }

    private List<Node> ParseBody() {
        var depth = Previous().Value;
        var body = new List<Node>();
        while (!IsAtEnd()) {
            if (!Match(TokenType.CloseBrace, depth)) {
                body.Add(ParseLine());
            } else {
                return body;
            }
        }
        throw new SyntaxErrorException("Expect '}' to end method at " + Peek().Position);
    }

    private Node ParseLine() {
        if (Match(TokenType.If)) {
            return ParseIf();
        } else if (Match(TokenType.VarDef)) {
            return ParseVar();
        } else if (Match(TokenType.Public)) {
            return ParsePublic();
        } else if (Match(TokenType.Identifier)) {
            var idenifier = ParseIdentifier();
            Consume(TokenType.Semicolon, "';' expected");
            return idenifier;
        } else if (Match(TokenType.MethodCall)) {
            var methodcall = ParseMethodCall();
            Consume(TokenType.Semicolon, "';' expected");
            return methodcall;
        } else if (Match(TokenType.Return)) {
            var arguments = new List<Node>();
            Consume(TokenType.OpenParenthesis, "'(' expected");
            while(!Match(TokenType.CloseParenthesis)) {
                arguments.Add(ParseExpression());
                Match(TokenType.Comma);
            }
            Consume(TokenType.Semicolon, "';' expected");
            return new ReturnNode(arguments);
        }
        throw new SyntaxErrorException("What is `" + Peek().Value + "' at " + Peek().Position);
    }

    private Node ParseMethodCall() {
        var classish = Previous();
        Consume(TokenType.Dot, "what the heck happened");
        var method = Consume(TokenType.Identifier, "Expected Method name");

        var arguments = new List<Node>();
        var depth = Consume(TokenType.OpenParenthesis, "Expected '(' after method call");
        while(!Match(TokenType.CloseParenthesis, depth.Value)) {
            arguments.Add(ParseExpression());
            Match(TokenType.Comma);
        }
        return new MethodCallNode(classish.Value, method.Value, arguments);
    }

    private Node ParseIdentifier() {
        var identifier = Previous();
        if (Match(TokenType.Plus)) {
            var oper = Previous();
            if (Match(TokenType.Plus)) {
                return new VarModificationNode(identifier.Value, new NumberNode(1), "+");
            }
            Consume(TokenType.Equal, "Invalid expression term");
            return new VarModificationNode(identifier.Value, ParseExpression(), oper.Value);
        } else if (Match(TokenType.Minus)) {
            var oper = Previous();
            if (Match(TokenType.Minus)) {
                return new VarModificationNode(identifier.Value, new NumberNode(1), "-");
            }
            Consume(TokenType.Equal, "Invalid expression term");
            return new VarModificationNode(identifier.Value, ParseExpression(), oper.Value);
        } else if (Match(TokenType.Multiply) || Match(TokenType.Divide) || Match(TokenType.Mod)) {
            var oper = Previous();
            Consume(TokenType.Equal, "Invalid expression term");
            return new VarModificationNode(identifier.Value, ParseExpression(), oper.Value);
        }
        Consume(TokenType.Equal, "Invalid expression term");
        return new VarModificationNode(identifier.Value, ParseExpression());
    }

    private Node ParseIdentifierVar() {
        var identifier = Previous();
        return new VarGetNode(identifier.Value);
    }


    private Node ParseIf() {
        Consume(TokenType.OpenParenthesis, "Expect '(' after 'if'.");
        Node condition = ParseExpression();
        Consume(TokenType.CloseParenthesis, "Expect ')' after if condition.");

        List<Node> thenBranch = new List<Node>();
        List<Node> elseBranch = null;
        if (Match(TokenType.OpenBrace)) {
            thenBranch = ParseBody();
        } else {
            thenBranch.Add(ParseLine());
        }

        if (Match(TokenType.Else)) {
            elseBranch = new List<Node>();
            if (Match(TokenType.OpenBrace)) {
                elseBranch = ParseBody();
            } else {
                elseBranch.Add(ParseLine());
            }
        }

        return new IfStatement(condition, thenBranch, elseBranch);
    }

    private Node ParseVar() {
        var variable = Previous();
        var identifier = Consume(TokenType.Identifier, "Expect variable name");
        if (Match(TokenType.Equal)) {
            var expression = ParseExpression();
            Consume(TokenType.Semicolon, "Expect ';'");
            return new VarDeclarationNode(variable.Value, identifier.Value, expression);
        } else {
            Consume(TokenType.Semicolon, "Expect ';'");
            return new VarDeclarationNode(variable.Value, identifier.Value);
        }
    }

    public Node ParseExpression() {
        var node = ParseTerm1();
        
        while (Match(TokenType.OrOr)) {
            Token operatorToken = Previous();
            Node right = ParseTerm1();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseTerm1() {

        var node = ParseTerm2();
        
        while (Match(TokenType.And)) {
            Token operatorToken = Previous();
            Node right = ParseTerm2();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseTerm2() {

        var node = ParseTerm3();
        
        while (Match(TokenType.EqualEqual)) {
            Token operatorToken = Previous();
            Node right = ParseTerm3();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseTerm3() {

        var node = ParseTerm4();
        
        while (Match(TokenType.Greater) || Match(TokenType.GreaterOrEqual) || Match(TokenType.Less) || Match(TokenType.LessOrEqual)) {
            Token operatorToken = Previous();
            Node right = ParseTerm4();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseTerm4() {

        var node = ParseTerm5();
        
        while (Match(TokenType.Plus) || Match(TokenType.Minus)) {
            Token operatorToken = Previous();
            Node right = ParseTerm5();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseTerm5() {

        var node = ParseTerm6();
        
        while (Match(TokenType.Multiply) || Match(TokenType.Divide) || Match(TokenType.Mod)) {
            Token operatorToken = Previous();
            Node right = ParseTerm6();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseTerm6() {

        var node = ParseFactor();
        
        while (Match(TokenType.Power)) {
            Token operatorToken = Previous();
            Node right = ParseFactor();
            node = new BinaryExpression(node, operatorToken, right);
        }

        return node;
    }

    private Node ParseFactor() {
        if (Match(TokenType.Dot)) {
            return new NumberNode(float.Parse("." + Consume(TokenType.Number, "Expect float").Value));
        }
        if (Match(TokenType.Number)) {
            return new NumberNode(float.Parse(Previous().Value));
        } else if (Match(TokenType.OpenParenthesis)) {
            Node expression = ParseExpression();
            Consume(TokenType.CloseParenthesis, "Expect ')' after expression.");
            return expression;
        } else if (Match(TokenType.Identifier)) {
            return ParseIdentifierVar();
        } else if (Match(TokenType.Minus)) {
            return new NegateNode(ParseFactor());
        } else if (Match(TokenType.Not)) {
            return new NotNode(ParseFactor());
        } else if (Match(TokenType.New)) {
            var type = Consume(TokenType.VarDef, "Identifier Expected");
            
            var arguments = new List<Node>();
            var depth = Consume(TokenType.OpenParenthesis, "Expected '(' after method call");
            while(!Match(TokenType.CloseParenthesis, depth.Value)) {
                arguments.Add(ParseExpression());
                Match(TokenType.Comma);
            }
            return new NewNode(type.Value, arguments);
        } else if (Match(TokenType.MethodCall)) {
            return ParseMethodCall();
        }
        throw new SyntaxErrorException("Unknown factor '" + Peek().Value + "' at " + Peek().Position);
    }

    private List<Node> ParseNewArguments() {
        var arguments = new List<Node>();
        if (Match(TokenType.CloseParenthesis)) {
            return arguments;
        }
        while (!IsAtEnd()) {
            var argument = Consume(TokenType.VarDef, "Expect Type");
            string typeName = argument.Value;
            var identifierToken = Consume(TokenType.Identifier, "Expect variable name");
            arguments.Add(new VarDeclarationNode(typeName, identifierToken.Value));
            if (Match(TokenType.CloseParenthesis)) {
                return arguments;
            }
            Consume(TokenType.Comma, "Expect ',' between arguments");
        }
        throw new SyntaxErrorException("Expect ')' after arguments at " + Peek().Position);
    }

    private Node ParseGlobalDefinition() {
        var type = Previous();
        var identifierToken = Consume(TokenType.Identifier, "Expect variable name");
        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new VarDeclarationNode(type.Value, identifierToken.Value);
    }

    private bool Check(TokenType type, string value = "") {
        if (IsAtEnd()) return false;
        if (value == "") return Peek().Type == type;
        else return Peek().Type == type && Peek().Value == value;
    }

    private Token Advance() {
        if (!IsAtEnd()) currentPosition++;
        return Previous();
    }

    private bool IsAtEnd() {
        return Peek().Type == TokenType.EOF;
    }

    private Token Peek() {
        return tokens[currentPosition];
    }

    private Token Previous() {
        return tokens[currentPosition - 1];
    }

    private Token Consume(TokenType type, string errorMessage) {
        if (Check(type)) return Advance();
        throw new Exception(errorMessage + " at " + Peek().Value + " at position " + Peek().Position);
    }

    private bool Match(TokenType type, string value = "") {
        if (Check(type, value)) {
            Advance();
            return true;
        }
        return false;
    }
}