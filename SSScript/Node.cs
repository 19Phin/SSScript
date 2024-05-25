public abstract class Node {}

public class BinaryExpression : Node {
    public Node Left { get; } 
    public Token Operator { get; } 
    public Node Right { get; } 

    public BinaryExpression(Node left, Token operatorToken, Node right) {
        Left = left;
        Operator = operatorToken;
        Right = right;
    }
}

public class IfStatement : Node {
    public Node Condition { get; }
    public List<Node> ThenBranch { get; }
    public List<Node> ElseBranch { get; }

    public IfStatement(Node condition, List<Node> thenBranch, List<Node> elseBranch = null) {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }
}

public class MethodCallNode : Node {
    public string MethodClass { get; }
    public string MethodName { get; }
    public List<Node> Arguments { get; }

    public MethodCallNode(string methodClass, string methodName, List<Node> arguments) {
        MethodClass = methodClass;
        MethodName = methodName;
        Arguments = arguments;
    }
}

public class MethodNode : Node {
    public string MethodName { get; }
    public List<Node> Arguments { get; }
    public List<Node> Body { get; }

    public MethodNode(string methodName, List<Node> arguments, List<Node> body) {
        MethodName = methodName;
        Arguments = arguments;
        Body = body;
    }
}

public class NotNode : Node {
    public Node Node { get; }

    public NotNode(Node node) {
        Node = node;
    }
}

public class NegateNode : Node {
    public Node Node { get; }

    public NegateNode(Node node) {
        Node = node;
    }
}

public class NewNode : Node {
    public string Type { get; }
    public List<Node> Arguments { get; }

    public NewNode(string type, List<Node> arguments) {
        Type = type;
        Arguments = arguments;
    }
}

public class ReturnNode : Node {
    public List<Node> Arguments { get; }

    public ReturnNode(List<Node> arguments) {
        Arguments = arguments;
    }
}

public class VarDeclarationNode : Node {
    public string TypeName { get; }
    public string Identifier { get; }
    public Node Expression { get; }
    public bool IsPublic { get; set; }

    public VarDeclarationNode(string typeName, string identifier, Node expression = null, bool isPublic = false) {
        TypeName = typeName;
        Identifier = identifier;
        Expression = expression;
        IsPublic = isPublic;
    }

    public VarDeclarationNode(string typeName, string identifier, bool isPublic)
        : this(typeName, identifier, null, isPublic) { }
}

public class VarModificationNode : Node {
    public string Identifier { get; }
    public Node Expression { get; }

    public string Operator { get; }

    public VarModificationNode(string identifier, Node expression, string operatorToken = "=") {
        Identifier = identifier;
        Expression = expression;
        Operator = operatorToken;
    }
}

public class VarGetNode : Node {
    public string Identifier { get; }

    public VarGetNode(string identifier) {
        Identifier = identifier;
    }
}

public class NumberNode : Node {
    public float Value { get; }

    public NumberNode(float value) {
        Value = value;
    }
}

public class BaseNode : Node {
    public List<Node> Nodes { get; }

    public BaseNode(List<Node> nodes) {
        Nodes = nodes;
    }
}