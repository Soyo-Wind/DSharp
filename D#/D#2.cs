
using System.Text;

class DS2
{
    private static List<string> IncludedHeaders = new();
    private static bool inFunction = false;
    private static readonly Stack<string> mainBlockStack = new();
    private static readonly Stack<string> funcBlockStack = new();
    private static int funcIndent = 0;
    private static int currentIndent = 0; // mainは初期インデント1で開始
    private static bool log_;

    public static void Main(string[] args)
    {
        // 制御構文のリスト
        HashSet<string> controlKeywords = new() { "if", "elif", "else", "el", "for", "while", "each", "func" };
        try
        {
            string code = File.ReadAllText(args[0]);
            log_ = args[1] == "t" ? true : false; // ログ出力の有無
            string[] lines = code.Replace("\r", "").Split('\n');
            bool inConditionalBlock = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) continue;

                int indentCount = Tools.CountIndent(line);
                string trimmedLine = line.Substring(indentCount * 4);
                string[] tokens = Split(trimmedLine, i + 1);

                // インデント0で制御構文以外の場合、条件ブロックをリセット
                if (!controlKeywords.Contains(tokens[0]) && indentCount == 0)
                {
                    inConditionalBlock = false;
                    if (inFunction)
                    {
                        // funcブロックの終了
                        while (funcIndent > 0)
                        {
                            Tools.funcDefs.Append("}\n");
                            Tools.FuncOpenBraces--;
                            funcIndent--;
                            if (funcBlockStack.Count > 0) funcBlockStack.Pop();
                        }
                        inFunction = false;
                    }
                }

                // インデント減少処理
                if (inFunction)
                {
                    if (indentCount < funcIndent)
                    {
                        while (indentCount < funcIndent)
                        {
                            Tools.funcDefs.Append("}\n");
                            Tools.FuncOpenBraces--;
                            funcIndent--;
                            if (funcBlockStack.Count > 0) funcBlockStack.Pop();
                        }
                        if (funcIndent == 0) inFunction = false;
                    }
                }
                else
                {
                    if (indentCount < currentIndent && mainBlockStack.Count > 0)
                    {
                        while (currentIndent > indentCount && mainBlockStack.Count > 0)
                        {
                            Tools.cpplang.Append("}\n");
                            Tools.MainOpenBraces--;
                            currentIndent--;
                            mainBlockStack.Pop();
                        }

                    }
                }
                // ブロック開始の処理を先に行う（←これが重要）
                if (controlKeywords.Contains(tokens[0]))
                {
                    if (inFunction)
                    {
                        funcBlockStack.Push(tokens[0]);
                        funcIndent++;
                        Tools.FuncOpenBraces++;
                    }
                    else
                    {
                        mainBlockStack.Push(tokens[0]);
                        currentIndent++;
                        Tools.MainOpenBraces++;
                    }
                    if (tokens[0] == "if" || tokens[0] == "elif") inConditionalBlock = true;
                    if (tokens[0] == "func") inFunction = true;
                }

                // その後にコード変換を行う（←ブロック内として扱われる）
                ConvertToCpp(tokens, trimmedLine, ref inConditionalBlock, ref inFunction, i + 1);

            }

            // 残りのfuncブロックを閉じる
            while (funcIndent > 0)
            {
                Tools.funcDefs.Append("}\n");
                funcIndent--;
                Tools.FuncOpenBraces--;
                if (funcBlockStack.Count > 0) funcBlockStack.Pop();
            }

            // 残りのmainブロックを閉じる
            while (mainBlockStack.Count > 0)
            {
                Tools.cpplang.Append("}\n");
                Tools.MainOpenBraces--;
                mainBlockStack.Pop();
            }

            // コード結合
            string fullCode = Tools.includes.ToString() + Tools.funcDefs.ToString() + Tools.cpplang.ToString() + Tools.finlang;
            File.WriteAllText("main.cpp", fullCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string[] Split(string a, int lineNumber)
    {
        a = a.TrimEnd() + " ";
        List<string> strings = new();
        int start = 0;
        bool inScape = false;
        int parenDepth = 0;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == ' ' && parenDepth == 0 && !inScape)
            {
                if (i > start)
                    strings.Add(a[start..i]);
                start = i + 1;
            }
            else if (a[i] == '"' && !inScape)
            {
                i++;
                while (i < a.Length && a[i] != '"')
                {
                    i++;
                }
                strings.Add(a[start..i]);
                start = i;
            }
            else if (a[i] == '\'' && !inScape)
            {
                i++;
                while (i < a.Length && a[i] != '\'')
                {
                    i++;
                }
                strings.Add(a[start..i]);
                start = i;
            }
            else if (a[i] == '(')
            {
                parenDepth = 1;
                i++;
                while (i + 1 < a.Length && parenDepth > 0)
                {
                    if (a[i] == '(') parenDepth++;
                    else if (a[i] == ')') parenDepth--;
                    i++;
                }
                strings.Add(a[start..i]);
                start = i;
            }
        }
        if (parenDepth > 0)
            throw new Exception($"Unclosed parenthesis at line {lineNumber}");
        if (start < a.Length - 1)
            strings.Add(a[start..(a.Length - 1)]);

        return strings.ToArray();
    }

    private static void ConvertToCpp(string[] tokens, string line, ref bool inConditionalBlock, ref bool inFunction, int lineNumber)
    {
        Console.Write(log_ ? $"Line {lineNumber}: indent={Tools.CountIndent(line)}, \ncurrentIndent={currentIndent}, \nfuncIndent={funcIndent}, \nmainBlockStack.Count={mainBlockStack.Count}, \nfuncBlockStack.Count={funcBlockStack.Count}\n\n" : "");
        string ev = tokens[0];
        StringBuilder targetBuffer = inFunction ? Tools.funcDefs : Tools.cpplang;
        try
        {
            if (ev == "def")
            {
                if (tokens.Length != 4) throw new Exception($"Invalid def syntax at line {lineNumber}: Expected 'def <type> <name> <value>'");
                string type = tokens[1];
                string name = tokens[2];
                string value = tokens[3];
                string ctype = Tools.Ctype(type, lineNumber);
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append(type.Split(':')[0] == "lis" ? $"{ctype} {name}{value};\n" : $"{ctype} {name} = {value};\n");
            }
            // 他のコマンドは変更なし
            else if (ev == "q")
            {
                if (tokens.Length != 3) throw new Exception($"Invalid q syntax at line {lineNumber}: Expected 'q <name> <value>'");
                string name = tokens[1];
                string value = tokens[2];
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append($"{name} = {value};\n");
            }
            else if (ev == "add")
            {
                if (tokens.Length != 3) throw new Exception($"Invalid add syntax at line {lineNumber}: Expected 'add <name> <value>'");
                string name = tokens[1];
                string value = tokens[2];
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append($"{name}.push_back({value});\n");
            }
            else if (ev == "del")
            {
                if (tokens.Length != 3) throw new Exception($"Invalid del syntax at line {lineNumber}: Expected 'del <name> <position>'");
                string name = tokens[1];
                string pos = tokens[2];
                targetBuffer.Append($"{name}.erase({name}.begin() + {pos});\n");
            }
            else if (ev == "ins")
            {
                if (tokens.Length != 4) throw new Exception($"Invalid ins syntax at line {lineNumber}: Expected 'ins <name> <position> <value>'");
                string name = tokens[1];
                string pos = tokens[2];
                string value = tokens[3];
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append($"{name}.insert({name}.begin() + {pos}, {value});\n");
            }
            else if (ev == "outln")
            {
                if (tokens.Length < 2) throw new Exception($"Invalid outln syntax at line {lineNumber}: Expected 'outln <value>'");
                string value = string.Join(" ", tokens[1..]);
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append($"cout << {value} << endl;\n");
            }
            else if (ev == "out")
            {
                if (tokens.Length < 2) throw new Exception($"Invalid out syntax at line {lineNumber}: Expected 'out <value>'");
                string value = string.Join(" ", tokens[1..]);
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append($"cout << {value};\n");
            }
            else if (ev == "in")
            {
                if (tokens.Length != 2) throw new Exception($"Invalid in syntax at line {lineNumber}: Expected 'in <variable>'");
                string name = tokens[1];
                targetBuffer.Append($"cin >> {name};\n");
            }
            else if (ev == "if" || ev == "elif")
            {
                if (tokens.Length < 2) throw new Exception($"Invalid {ev} syntax at line {lineNumber}: Expected '{ev} <condition>'");
                string condition = tokens[1];
                targetBuffer.Append($"{(ev == "if" ? "if" : "else if")} ({condition}) {{\n");
                if (inFunction) Tools.FuncOpenBraces++;
                else Tools.MainOpenBraces++;
            }
            else if (ev == "else" || ev == "el")
            {
                if (!inConditionalBlock) throw new Exception($"else/el without preceding if/elif at line {lineNumber}: {line}");
                if (tokens.Length > 1) throw new Exception($"Invalid else/el syntax at line {lineNumber}: Expected 'else' or 'el'");
                targetBuffer.Append($"else {{\n");
                if (inFunction) Tools.FuncOpenBraces++;
                else Tools.MainOpenBraces++;
            }
            else if (ev == "for")
            {
                if (tokens.Length != 5) throw new Exception($"Invalid for syntax at line {lineNumber}: Expected 'for <var> <start> <condition> <increment>'");
                string varName = tokens[1];
                string start = tokens[2];
                string condition = tokens[3];
                string increment = tokens[4];
                if (start[0] == '(' && start[^1] == ')')
                {
                    start = start[1..^1];
                }
                if (condition[0] == '(' && condition[^1] == ')')
                {
                    condition = condition[1..^1];
                }
                if (increment[0] == '(' && increment[^1] == ')')
                {
                    increment = increment[1..^1];
                }
                targetBuffer.Append($"for (int {varName} = {start}; {condition}; {varName} += {increment}) {{\n");
                if (inFunction) Tools.FuncOpenBraces++;
                else Tools.MainOpenBraces++;
            }
            else if (ev == "while")
            {
                if (tokens.Length < 2) throw new Exception($"Invalid while syntax at line {lineNumber}: Expected 'while <condition>'");
                string condition = string.Join(" ", tokens[1..]);
                if (condition[0] == '(' && condition[^1] == ')')
                {
                    condition = condition[1..^1];
                }
                targetBuffer.Append($"while ({condition}) {{\n");
                if (inFunction) Tools.FuncOpenBraces++;
                else Tools.MainOpenBraces++;
            }
            else if (ev == "each")
            {
                if (tokens.Length != 3) throw new Exception($"Invalid each syntax at line {lineNumber}: Expected 'each <var> <container>'");
                string varName = tokens[1];
                string container = tokens[2];
                if (container[0] == '(' && container[^1] == ')')
                {
                    container = container[1..^1];
                }
                targetBuffer.Append($"for (auto& {varName} : {container}) {{\n");
                if (inFunction) Tools.FuncOpenBraces++;
                else Tools.MainOpenBraces++;
            }
            else if (ev == "func")
            {
                if (tokens.Length < 4 || (tokens.Length - 3) % 2 != 0) throw new Exception($"Invalid func syntax at line {lineNumber}: Expected 'func <return_type> <name> <type1> <arg1> <type2> <arg2> ...'");
                string returnType = Tools.Ctype(tokens[1], lineNumber);
                string funcName = tokens[2];
                if (Tools.DefinedFunctions.Contains(funcName))
                    throw new Exception($"Function {funcName} already defined at line {lineNumber}");
                Tools.DefinedFunctions.Add(funcName);
                List<string> args = new();
                for (int j = 3; j < tokens.Length; j += 2)
                {
                    string argType = Tools.Ctype(tokens[j], lineNumber);
                    string argName = tokens[j + 1];
                    args.Add($"{argType} {argName}");
                }
                string argList = string.Join(", ", args);
                Tools.funcDefs.Append($"{returnType} {funcName}({argList}) {{\n");
                Tools.FuncOpenBraces++;
                funcIndent = 1;
            }
            else if (ev == "return")
            {
                if (tokens.Length != 2) throw new Exception($"Invalid return syntax at line {lineNumber}: Expected 'return <value>'");
                string value = tokens[1];
                if (value[0] == '(' && value[^1] == ')')
                {
                    value = value[1..^1];
                }
                targetBuffer.Append($"return {value};\n");
            }
            else if (ev == "quit")
            {
                if (tokens.Length != 2) throw new Exception($"Invalid quit syntax at line {lineNumber}: Expected 'quit <code>'");
                string quitcode = tokens[1];
                targetBuffer.Append($"QUITCODE = {quitcode};\ngoto QUITLABEL;\n");
            }
            else if (ev == "use")
            {
                if (tokens.Length != 2) throw new Exception($"Invalid use syntax at line {lineNumber}: Expected 'use <header>'");
                string header = tokens[1];
                if (!Tools.ValidHeaders.Contains(header) && !IncludedHeaders.Contains(header))
                {
                    throw new Exception($"Invalid header: {header} at line {lineNumber}");
                }
                if (!IncludedHeaders.Contains(header))
                {
                    IncludedHeaders.Add(header);
                    Tools.includes.Append($"#include <{header}>\n");
                }
            }
            else if (ev == "break")
            {
                if (tokens.Length != 1) throw new Exception($"Invalid break syntax at line {lineNumber}: Expected 'break'");
                targetBuffer.Append("break;\n");
            }
            else if (ev == "continue")
            {
                if (tokens.Length != 1) throw new Exception($"Invalid continue syntax at line {lineNumber}: Expected 'continue'");
                targetBuffer.Append("continue;\n");
            }
            else if (ev == "call")
            {
                if (tokens.Length < 2) throw new Exception($"Invalid call syntax at line {lineNumber}: Expected 'call <function_name> [args...]'");
                string func = tokens[1];
                if (func[0] == '(' && func[^1] == ')')
                {
                    func = func[1..^1];
                }
                targetBuffer.Append(func+";\n");
            }
            else
            {
                throw new Exception($"Unknown command: {ev} at line {lineNumber}, line: {line}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error at line {lineNumber}: {line}\n{ex.Message}");
            targetBuffer.Append($"/* Error: {ex.Message} */\n");
        }
    }
}
class Tools
{
    // 生成されるC++コードのバッファ
    internal static StringBuilder cpplang = new(@"
int main() {
int QUITCODE = 0;
");
    internal static StringBuilder includes = new("#include <bits/stdc++.h>\nusing namespace std;\n");
    internal static StringBuilder funcDefs = new();
    internal static readonly string finlang = @"goto QUITLABEL;
QUITLABEL: return QUITCODE;
}
";
    internal static readonly HashSet<string> ValidHeaders = new HashSet<string>
    {
        "iostream", "vector", "string", "cmath", "algorithm", "array", "deque", "list",
    };

    // 定義済み関数の管理
    internal static readonly HashSet<string> DefinedFunctions = new HashSet<string>();

    // ブロックの整合性チェック用
    internal static int MainOpenBraces = 0; // mainの初期"{"
    internal static int FuncOpenBraces = 0;

    internal static readonly Dictionary<string, string> TypeMap = new()
    {
        { "rin", "string" },
        { "teg", "int" },
        { "cim", "double" },
        { "tnil", "bool" },
        {"lis","vector"}
    };

    internal static int CountIndent(string line)
    {
        int spaceCount = 0;
        foreach (char c in line)
        {
            if (c == ' ') spaceCount++;
            else if (c == '\t') spaceCount += 4; // タブもスペース4つ相当
            else break;
        }
        return spaceCount / 4;
    }


    internal static string Ctype(string type, int lineNumber)
    {
        if (string.IsNullOrEmpty(type)) throw new Exception($"Invalid type at line {lineNumber}");

        string[] parts = type.Split(':');
        string baseType = parts[^1]; // 最後の部分がベース型
        if (!TypeMap.TryGetValue(baseType, out var ctype))
            throw new Exception($"Unknown base type: {baseType} at line {lineNumber}");

        // ネストされたlisの数だけvectorを積む
        for (int j = parts.Length - 2; j >= 0; j--)
        {
            if (parts[j] != "lis")
                throw new Exception($"Invalid type format: {type} at line {lineNumber}. Expect 'lis:' repeated followed by base type.");
            ctype = $"vector<{ctype}>";
        }
        return ctype;
    }
}
