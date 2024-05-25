using System.Numerics;
using System.Text;

public class Converter
{
    private int _id = 1;
    private int _varId = 1;
    private int _lastExecutionID = -1;
    private readonly Dictionary<string, ScriptVariable> _scriptVars = new Dictionary<string, ScriptVariable>();

    private readonly List<SSModulePort> _inputPorts = new List<SSModulePort>();
    private readonly List<SSModulePort> _outputPorts = new List<SSModulePort>();

    private readonly List<SSNode> _nodes = new List<SSNode>();
    private readonly List<SSNodeConnection> _connections = new List<SSNodeConnection>();
    private readonly List<SSVariable> _variables = new List<SSVariable>();
    private readonly List<SSValue> _values = new List<SSValue>();

    private readonly List<string> _varList = new List<string> {
    "ls",
    "lt",
    "rt",
    "V2_Splitter",
    "Float_Invert",
    "+",
    "a_button",
    "b_button",
    "lb",
    "rb",
    "rs",
    "x_button",
    "y_button",
    "Bool_To_Float",
    "*",
    "Not",
    "V2_Builder",
    "V+",
    "V3_Builder",
    "V3.cross",
    "V/",
    "V3.dot",
    "V3.magnitude",
    "V*",
    "V3.normalize",
    "V3.project",
    "V3.project_on_plane",
    "V3_Splitter",
    "V-",
    "V3.up",
    "V3.camera_pivot_horizontal",
    "V3.camera_pivot_vertical",
    "V3.camera_tilt_horizontal",
    "V3.camera_tilt_vertical",
    "V3.angle",
    "Math.sign",
    "a_key",
    "ctrl_key",
    "d_key",
    "e_key",
    "q_key",
    "shift_key",
    "s_key",
    "space_key",
    "w_key",
    "/",
    "If",
    "Math.delta_Time",
    "<",
    "<=",
    ">",
    ">=",
    "Math.sin",
    "Math.cos",
    "Math.tan",
    "Math.clamp",
    "Math.clamp_01",
    "Math.float_lerp",
    "%",
    "**",
    "-",
    "Math.abs",
    "wasd",
    "mouse_1",
    "mouse_2",
    "&&",
    "||",
    "XOR",
    "==",
    "Ternary_Operator",
    "SS.toggleNode",
    "SS.trigger",
    "Math.sqrt",
    "Math.acos",
    "Math.asin",
    "Math.atan",
    "b_key",
    "c_key",
    "f_key",
    "g_key",
    "h_key",
    "i_key",
    "j_key",
    "k_key",
    "l_key",
    "m_key",
    "n_key",
    "o_key",
    "p_key",
    "r_key",
    "t_key",
    "u_key",
    "v_key",
    "x_key",
    "y_key",
    "z_key",
    "num_0_key",
    "num_1_key",
    "num_2_key",
    "num_3_key",
    "num_4_key",
    "num_5_key",
    "num_6_key",
    "num_7_key",
    "num_8_key",
    "num_9_key"
    };

    private readonly Dictionary<string, int> _varIndexMap = new Dictionary<string, int>();

    public static void Main(string[] args) {
        var converter = new Converter();
        try {
            converter.ProcessFile("program.txt");
        }
        catch (Exception ex) {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private FileInfo FindCreationJson()
    {
        var jsonFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.json");

        var filteredFiles = jsonFiles.Where(file => 
            !file.EndsWith("SSScript.deps.json", StringComparison.OrdinalIgnoreCase) &&
            !file.EndsWith("SSScript.runtimeconfig.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filteredFiles.Count != 1)
        {
            throw new InvalidOperationException("There are to many Json files, unable to identify Creation.");
        }

        return new FileInfo(filteredFiles.First());
    }

    private void ProcessFile(string filePath) {
        try {
            var creationFile = FindCreationJson();
            _varId = GetfileID(creationFile);
            string sourceCode = File.ReadAllText(filePath);

            Lexer lexer = new Lexer(sourceCode);
            List<Token> tokens = lexer.Tokenize();

            Parser parser = new Parser(tokens);
            BaseNode ast = parser.Parse();

            for (int i = 0; i < _varList.Count; i++) {
                _varIndexMap[_varList[i]] = i;
            }
            Convert(ast);
            ReplaceData(Print(), creationFile);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error processing file: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    public int GetfileID(FileInfo creationFile) {
        string creationJson = File.ReadAllText(creationFile.Name);
        int startIndex = creationJson.IndexOf("\"refIDCounter\":") + "\"refIDCounter\":".Length;
        int endIndex = creationJson.IndexOf(",", startIndex);

        string currentId = creationJson.Substring(startIndex, endIndex - startIndex);
        return int.Parse(currentId.Trim());
    }

    public void ReplaceData(string replacement, FileInfo creationFile) {
        string creationJson = File.ReadAllText(creationFile.Name);

        int startIndex = creationJson.IndexOf("\"refIDCounter\":") + "\"refIDCounter\":".Length;
        int endIndex = creationJson.IndexOf(",", startIndex);

        string before = creationJson.Substring(0, startIndex);
        string after = creationJson.Substring(endIndex);

        creationJson = before + _varId + after;

        startIndex = creationJson.IndexOf("\"NodeGraph\": {");
        endIndex = creationJson.IndexOf("\"pieces\": [", startIndex);

        before = creationJson.Substring(0, startIndex);
        after = creationJson.Substring(endIndex);

        string result = before + replacement + after;

        Console.WriteLine(result);

        File.WriteAllText(creationFile.Name, result);
    }

    public void Convert(BaseNode ast) {
        try {
            foreach (Node node in ast.Nodes) {
                if (node is VarDeclarationNode varDecl) {
                    HandleVarDeclaration(varDecl);
                }
                else if (node is MethodNode methodNode) {
                    HandleMethodNode(methodNode);
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error converting node: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private void HandleVarDeclaration(VarDeclarationNode varNode) {
        string typeName = CapitalizeFirstLetter(varNode.TypeName);
        _variables.Add(new SSVariable(varNode.Identifier, _varId, typeName, GetDefaultValue(varNode.TypeName)));
        _scriptVars[varNode.Identifier] = new ScriptVariable(varNode.TypeName, _varId, true);
        _varId++;
    }

    private void HandleMethodNode(MethodNode methodNode) {
        if (!methodNode.MethodName.Equals("Main"))
            throw new Exception("Only main method allowed");

        int paramIndex = 0;
        foreach (VarDeclarationNode argument in methodNode.Arguments) {
            _inputPorts.Add(new SSModulePort(argument.Identifier));
            _scriptVars[argument.Identifier] = new ScriptVariable(argument.TypeName, -1, paramIndex++);
        }
        ConvertBody(methodNode.Body);
    }

    public void ConvertBody(List<Node> body, ScriptVariable condition = null) {
        foreach (Node line in body) {
            try {
                switch (line) {
                    case IfStatement ifStmt:
                        HandleIfStatement(ifStmt, condition);
                        break;
                    case VarDeclarationNode varDecl:
                        if (varDecl.IsPublic) {
                            int currentId = _id++;
                            _values.Add(new SSValue(currentId, 0, new Vector2(-200, -100), varDecl.Identifier));
                            _scriptVars[varDecl.Identifier] = new ScriptVariable(varDecl.TypeName, true, currentId);
                        } else {
                            _scriptVars[varDecl.Identifier] = Traverse(varDecl.Expression);
                        }
                        break;
                    case VarModificationNode varModNode:
                        if (_scriptVars[varModNode.Identifier].Global) {
                            var newValue = Traverse(varModNode.Expression);
                            int currentId = _id++;
                            if (condition != null) {
                                _nodes.Add(new SSNode(-3, currentId, default, 1));
                                currentId = _id++;
                                if (newValue.IsPublic) {
                                    _nodes.Add(new SSNode(69, currentId, new Vector2(-100, -100)));
                                } else {
                                    _nodes.Add(new SSNode(69, currentId));
                                }
                                _connections.Add(new SSNodeConnection(condition, currentId, 0));
                                _connections.Add(new SSNodeConnection(newValue, currentId, 1));
                                _connections.Add(new SSNodeConnection(currentId -1, 0, currentId, 2));
                                newValue = new ScriptVariable(newValue.Type, currentId);
                                currentId = _id++;
                            }
                            _nodes.Add(new SSNode(-4, currentId, new Vector2(0, 100), _scriptVars[varModNode.Identifier].ID));
                            _connections.Add(new SSNodeConnection(newValue, currentId, 0));
                            _connections.Add(new SSNodeConnection(_lastExecutionID, 0, currentId, 0, true));
                            _lastExecutionID = currentId;
                        }
                        else {
                            int currentId = _id++;
                            var newValue = Traverse(varModNode.Expression);
                            if (condition != null) {
                                if (newValue.IsPublic) {
                                    _nodes.Add(new SSNode(69, currentId, new Vector2(-100, -100)));
                                } else {
                                    _nodes.Add(new SSNode(69, currentId));
                                }
                                _connections.Add(new SSNodeConnection(condition, currentId, 0));
                                _connections.Add(new SSNodeConnection(newValue, currentId, 1));
                                _connections.Add(new SSNodeConnection(_scriptVars[varModNode.Identifier], currentId, 2));
                                newValue = new ScriptVariable(newValue.Type, currentId);
                            }
                            _scriptVars[varModNode.Identifier] =  newValue;
                        }
                        break;
                    case ReturnNode returnNode:
                        int paramIndex = 0;
                        foreach (Node argument in returnNode.Arguments) {
                            _outputPorts.Add(new SSModulePort((paramIndex + 1).ToString()));
                            _connections.Add(new SSNodeConnection(Traverse(argument), -1, paramIndex++));
                        }
                        _connections.Add(new SSNodeConnection(_lastExecutionID, 0, -1, 0, true));
                        _lastExecutionID = -1;
                        break;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error converting body line: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

    private void HandleIfStatement(IfStatement ifStmt, ScriptVariable condition) {
        var conditionVar = Traverse(ifStmt.Condition);
        var finalconditionVar = conditionVar;
        if (condition != null) {
            int currentId = _id++;
            _nodes.Add(new SSNode(65, currentId));
            _connections.Add(new SSNodeConnection(condition, currentId, 0));
            _connections.Add(new SSNodeConnection(conditionVar, currentId, 1));
            finalconditionVar = new ScriptVariable("bool", currentId);
        }
        ConvertBody(ifStmt.ThenBranch, finalconditionVar);
        if (ifStmt.ElseBranch != null) {
            int currentId = _id++;
            if (conditionVar.IsPublic) {
                _nodes.Add(new SSNode(15, currentId, new Vector2(-100, -100)));
            } else {
                _nodes.Add(new SSNode(15, currentId));
            }
            _connections.Add(new SSNodeConnection(conditionVar, currentId, 0));
            conditionVar = new ScriptVariable("bool", currentId);
            finalconditionVar = conditionVar;
            if (condition != null) {
                currentId = _id++;
                _nodes.Add(new SSNode(65, currentId));
                _connections.Add(new SSNodeConnection(condition, currentId, 0));
                _connections.Add(new SSNodeConnection(conditionVar, currentId, 1));
                finalconditionVar = new ScriptVariable("bool", currentId);
            }
            ConvertBody(ifStmt.ElseBranch, finalconditionVar);
        }
    }

    public ScriptVariable Traverse(Node node) {
        try {
            return node switch {
                BinaryExpression binaryExpr => VisitBinaryExpression(binaryExpr),
                NegateNode negateNode => VisitNegateNode(negateNode),
                NotNode notNode => VisitNotNode(notNode),
                NumberNode number => VisitNumberNode(number),
                VarGetNode varGetNode => VisitVarGetNode(varGetNode),
                MethodCallNode methodCallNode => VisitMethodCallNode(methodCallNode),
                NewNode newNode => VisitNewNode(newNode),
                _ => throw new Exception("Unsupported node type in expression")
            };
        }
        catch (Exception ex) {
            throw new Exception($"Error traversing node: {ex.Message}", ex);
        }
    }

    private ScriptVariable VisitBinaryExpression(BinaryExpression node) {
        var left = Traverse(node.Left);
        var right = Traverse(node.Right);
        int currentId = _id++;
        string type = "float";
        int typeId;

        if (left.Type.Equals("Vector3") || right.Type.Equals("Vector3")) {
            _varIndexMap.TryGetValue("V" + node.Operator.Value, out typeId);
            type = "Vector3";
        }
        else {
            if (left.Type.Equals("bool") || right.Type.Equals("bool")) {
                type = "bool";
            }
            _varIndexMap.TryGetValue(node.Operator.Value, out typeId);
        }
        if (left.IsPublic || right.IsPublic) {
            _nodes.Add(new SSNode(typeId, currentId, new Vector2(-100, -100)));
        } else {
            _nodes.Add(new SSNode(typeId, currentId));
        }
        _connections.Add(new SSNodeConnection(left, currentId, 0));
        _connections.Add(new SSNodeConnection(right, currentId, 1));
        return new ScriptVariable(type, currentId);
    }

    private ScriptVariable VisitNegateNode(NegateNode node) {
        var inside = Traverse(node.Node);
        int currentId = _id++;
        if (inside.IsPublic) {
            _nodes.Add(new SSNode(4, currentId, new Vector2(-100, -100)));
        } else {
            _nodes.Add(new SSNode(4, currentId));
        }
        _connections.Add(new SSNodeConnection(inside, currentId, 0));
        return new ScriptVariable("bool", currentId);
    }

    private ScriptVariable VisitNotNode(NotNode node) {
        var inside = Traverse(node.Node);
        int currentId = _id++;
        if (inside.IsPublic) {
            _nodes.Add(new SSNode(15, currentId, new Vector2(-100, -100)));
        } else {
            _nodes.Add(new SSNode(15, currentId));
        }
        _connections.Add(new SSNodeConnection(inside, currentId, 0));
        return new ScriptVariable("bool", currentId);
    }

    private ScriptVariable VisitNumberNode(NumberNode node) {
        int currentId = _id++;
        _values.Add(new SSValue(currentId, node.Value));
        return new ScriptVariable("float", currentId);
    }

    private ScriptVariable VisitVarGetNode(VarGetNode node) {
        if (_scriptVars.TryGetValue(node.Identifier, out var variable)) {
            if (variable.Global) {
                int currentId = _id++;
                _nodes.Add(new SSNode(-3, currentId, default, variable.ID));
                return new ScriptVariable(variable.Type, currentId, true);
            }
            else {
                return _scriptVars[node.Identifier];
            }
        } else if (_varIndexMap.TryGetValue(node.Identifier.ToLower(), out int typeId)) {
            int currentId = _id++;
            _nodes.Add(new SSNode(typeId, currentId));
            return new ScriptVariable("type", currentId);
        }

        throw new Exception($"Unknown variable '{node.Identifier}'");
    }

    private ScriptVariable VisitMethodCallNode(MethodCallNode methodCallNode) {
        if (_scriptVars.TryGetValue(methodCallNode.MethodClass, out var variable)) {
            int currentId = _id++;
            if (variable.Global) {
                _nodes.Add(new SSNode(-3, currentId, default, variable.ID));
                variable = new ScriptVariable(variable.Type, currentId, true);
                currentId = _id++;
            }
            int port = methodCallNode.MethodName switch {
                "X" => 0,
                "Y" => 1,
                "Z" => 2,
                _ => throw new Exception("Invalid method" + methodCallNode.MethodClass + "." + methodCallNode.MethodName)
            };

            int vectorTypeId = variable.Type.Equals("Vector3") ? 27 : 3;
            if (variable.IsPublic) {
                _nodes.Add(new SSNode(vectorTypeId, currentId, new Vector2(-100, -100)));
            } else {
                _nodes.Add(new SSNode(vectorTypeId, currentId));
            }
            _connections.Add(new SSNodeConnection(variable, currentId, 0));
            return new ScriptVariable("float", currentId, port);
        }

        string methodType = methodCallNode.MethodClass + "." + methodCallNode.MethodName.ToLower();
        if (!_varIndexMap.TryGetValue(methodType, out int methodTypeId)) {
            throw new Exception("unknown class or var `" + methodCallNode.MethodClass);
        }

        int methodCallId = _id++;

        int portIndex = 0;
        bool isPublic = false;
        foreach (Node argument in methodCallNode.Arguments) {
            var argVar = Traverse(argument);
            if (argVar.IsPublic) isPublic = true;
            _connections.Add(new SSNodeConnection(argVar, methodCallId, portIndex++));
        }
        if (isPublic) {
            _nodes.Add(new SSNode(methodTypeId, methodCallId, new Vector2(-100, -100)));
        } else {
            _nodes.Add(new SSNode(methodTypeId, methodCallId));
        }

        string returnType = methodCallNode.MethodName.ToLower() switch {
            "cross" => "Vector3",
            "dot" => "float",
            "magnitude" => "float",
            "normalize" => "Vector3",
            "project" => "Vector3",
            "project_On_Plane" => "Vector3",
            "up" => "Vector3",
            "camera_Pivot_Horizontal" => "Vector3",
            "camera_Pivot_Vertical" => "Vector3",
            "camera_Tilt_Horizontal" => "Vector3",
            "camera_Tilt_Vertical" => "Vector3",
            "angle" => "float",
            "sign" => "float",
            "delta_Time" => "float",
            "sin" => "float",
            "cos" => "float",
            "tan" => "float",
            "clamp" => "float",
            "clamp_01" => "float",
            "float_Lerp" => "float",
            "abs" => "float",
            "toggleNode" => "bool",
            "trigger" => "bool",
            "sqrt" => "float",
            "acos" => "float",
            "asin" => "float",
            "atan" => "float",
            _ => "Vector3"
        };

        return new ScriptVariable(returnType, methodCallId);
    }

    private ScriptVariable VisitNewNode(NewNode node) {
        int typeId = node.Type.Equals("Vector3") ? 18 : 16;
        string type = node.Type.Equals("Vector3") ? "Vector3" : "Vector2";

        int currentId = _id++;

        int portIndex = 0;
        bool isPublic = false;
        foreach (Node argument in node.Arguments) {
            var argVar = Traverse(argument);
            if (argVar.IsPublic) isPublic = true;
            _connections.Add(new SSNodeConnection(argVar, currentId, portIndex++));
        }
        if (isPublic) {
            _nodes.Add(new SSNode(typeId, currentId, new Vector2(-100, -100)));
        } else {
            _nodes.Add(new SSNode(typeId, currentId));
        }

        return new ScriptVariable(type, currentId);
    }

    private string GetDefaultValue(string typeName) {
        return typeName switch {
            "bool" => "false",
            "int" => "0",
            "float" => "0.0",
            "Vector2" => """{\"x\":0.0,\"y\":0.0}""",
            "Vector3" => """{\"x\":0.0,\"y\":0.0,\"z\":0.0}""",
            _ => throw new Exception("Unknown type {" + typeName + "}")
        };
    }

    private string CapitalizeFirstLetter(string input) {
        if (string.IsNullOrEmpty(input)) throw new ArgumentException("Input string cannot be null or empty");
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    private void identifyPublicOuts() {
        List<int> list = new List<int>();
        foreach(var connection in _connections) {
            if (!list.Contains(connection.FromNodeRefID))
            {
                list.Add(connection.FromNodeRefID);
            }
        }
        foreach(var node in _nodes) {
            if (!list.Contains(node.RefID)) {
                if (node.ID == -4) {
                    _connections.Add(new SSNodeConnection(node.RefID, 0, -1, 0, true));
                } else {
                    node.GraphPosition = new Vector2(200, -100);
                }
            }
        }
    }

    private string Print() {
        identifyPublicOuts();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($@"""NodeGraph"": {{
      ""graphName"": ""Untitled NodeGraph"",
      ""isMethod"": false,
      ""refID"": 0,
      ""refIDCounter"": {_id},
      ""ExecutionEnterPosition"": {{
        ""x"": -500.0,
        ""y"": 0.0
      }},
      ""ExecutionExitPosition"": {{
        ""x"": 500.0,
        ""y"": 0.0
      }},
      ""InputPorts"": [");
        if (_inputPorts.Count > 0) {
            foreach (SSModulePort port in _inputPorts) {
                sb.AppendLine(port.Print());
            }
            sb.Remove(sb.Length - 3, 2);
        }
        sb.AppendLine(@"
      ],
      ""OutputPorts"": [");
        if (_outputPorts.Count > 0) {
            foreach (SSModulePort port in _outputPorts) {
                sb.AppendLine(port.Print());
            }
            sb.Remove(sb.Length - 3, 2);
        }
        sb.AppendLine(@"
      ],
      ""ExecutionExits"": [],
      ""nodes"": [");
        if (_nodes.Count > 0) {
            foreach (SSNode node in _nodes) {
                sb.AppendLine(node.Print());
            }
            sb.Remove(sb.Length - 3, 2);
        }
        sb.AppendLine(@"
      ],
      ""nodeConnections"": [");
        if (_connections.Count > 0) {
            foreach (SSNodeConnection connection in _connections) {
                sb.AppendLine(connection.Print());
            }
            sb.Remove(sb.Length - 3, 2);
        }
        sb.AppendLine(@"
      ],
      ""nestedNodeGraphs"": [],
      ""Variables"": [");
        if (_variables.Count > 0) {
            foreach(SSVariable variable in _variables) {
                sb.AppendLine(variable.Print());
            }
            sb.Remove(sb.Length - 3, 2);
        }
        sb.AppendLine(@"
      ],
      ""Values"": [");
        if (_values.Count > 0) {
            foreach(SSValue value in _values) {
                sb.AppendLine(value.Print());
            }
            sb.Remove(sb.Length - 3, 2);
        }
        sb.Append(@"
      ]
    },
    ");
    return sb.ToString();
    }
}

public class ScriptVariable
{
    public string Type { get; }
    public int ID { get; }
    public int Port { get; }
    public bool Global { get; }
    public bool IsPublic { get; }

    public ScriptVariable(string type, int id, bool global = false) {
        Type = type;
        ID = id;
        Port = 0;
        Global = global;
    }

    public ScriptVariable(string type, int id, int port) { 
        Type = type;
        ID = id;
        Port = port;
        Global = false;
    }

    public ScriptVariable(string type, bool isPublic, int id = -1) { 
        Type = type;
        ID = id;
        Port = 0;
        Global = false;
        IsPublic = isPublic;
    }
}

public class SSNode
{
    public int ID { get; }
    public int RefID { get; }
    public int LinkedPieceRefID { get; }
    public Vector2 GraphPosition { get; set; }

    public SSNode(int id, int refID, Vector2 pos = default, int linkedPieceRefID = -1) {
        ID = id;
        RefID = refID;
        LinkedPieceRefID = linkedPieceRefID;
        GraphPosition = pos == default ? new Vector2(0, 0) : pos;
    }

    public string Print() {
        return $@"        {{
          ""ID"": [
            {ID}
          ],
          ""refID"": {RefID},
          ""linkedPieceRefID"": {LinkedPieceRefID},
          ""GraphPosition"": {{
            ""x"": {GraphPosition.X},
            ""y"": {GraphPosition.Y}
          }}
        }},";
    }
}

public class SSModulePort
{
    public string PortName { get; }
    public string DataType { get; }

    public SSModulePort(string portName) {
        PortName = portName;
        DataType = "Bool";
    }

    public string Print() {
        return $@"        {{
          ""portName"": ""{PortName}"",
          ""dataType"": ""{DataType}""
        }},";
    }
}

public class SSNodeConnection
{
    public bool IsExecutionPort { get; }
    public int FromNodeRefID { get; }
    public int FromNodePortIndex { get; }
    public int ToNodeRefID { get; }
    public int ToNodePortIndex { get; }

    public SSNodeConnection(int fromNodeRefID, int fromNodePortIndex, int toNodeRefID, int toNodePortIndex, bool isExecutionPort = false) {
        IsExecutionPort = isExecutionPort;
        FromNodeRefID = fromNodeRefID;
        FromNodePortIndex = fromNodePortIndex;
        ToNodeRefID = toNodeRefID;
        ToNodePortIndex = toNodePortIndex;
    }

    public SSNodeConnection(ScriptVariable from, int toNodeRefID, int toNodePortIndex)
        : this(from.ID, from.Port, toNodeRefID, toNodePortIndex) { }

    public string Print() {
        return $@"        {{
          ""isExecutionPort"": ""{IsExecutionPort}"",
          ""fromNodeRefID"": {FromNodeRefID},
          ""fromNodePortIndex"": {FromNodePortIndex},
          ""toNodeRefID"": {ToNodeRefID},
          ""toNodePortIndex"": {ToNodePortIndex}
        }},";
    }
}

public class SSVariable
{
    public string Name { get; }
    public int RefID { get; }
    public string Type { get; }
    public string Value { get; }

    public SSVariable(string name, int refID, string type, string value) {
        Name = name;
        RefID = refID;
        Type = type;
        Value = value;
    }
    public string Print() {
        return $@"        {{
          ""VariableName"": ""{Name}"",
          ""RefID"": {RefID},
          ""Type"": ""{Type}"",
          ""Value"": ""{Value}""
        }},";
    }
}

public class SSValue
{
    public int RefID { get; }
    public string Name { get; }
    public Vector2 GraphPosition { get; }
    public string Type { get; }
    public string Value { get; }

    public SSValue(int refID, float value, Vector2 pos = default, string name = "Value") {
        RefID = refID;
        Name = name;
        GraphPosition = pos == default ? new Vector2(0, 0) : pos;
        Type = "float";
        Value = value.ToString();
    }
    public string Print() {
        return $@"        {{
          ""refID"": {RefID},
          ""ValueName"": ""{Name}"",
          ""GraphPosition"": {{
            ""x"": {GraphPosition.X},
            ""y"": {GraphPosition.Y}
          }},
          ""Type"": ""{Type}"",
          ""Value"": ""{Value}""
        }},";
    }
}