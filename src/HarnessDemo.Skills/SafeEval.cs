namespace HarnessDemo.Skills;

/// <summary>
/// 安全的算术表达式求值器（递归下降解析，禁止反射/动态执行）。
/// 语法：expr := term (('+'|'-') term)* ; term := factor (('*'|'/') factor)* ; factor := number | '(' expr ')'
/// </summary>
internal static class SafeEval
{
    public static double Evaluate(string expression)
    {
        var p = new Parser(expression);
        var v = p.ParseExpr();
        p.ExpectEnd();
        return v;
    }

    private ref struct Parser
    {
        private readonly string _s;
        private int _i;

        public Parser(string s) { _s = s; _i = 0; }

        public double ParseExpr()
        {
            var v = ParseTerm();
            SkipSpace();
            while (Peek() == '+' || Peek() == '-')
            {
                var op = Eat();
                var r = ParseTerm();
                v = op == '+' ? v + r : v - r;
                SkipSpace();
            }
            return v;
        }

        private double ParseTerm()
        {
            var v = ParseFactor();
            SkipSpace();
            while (Peek() == '*' || Peek() == '/')
            {
                var op = Eat();
                var r = ParseFactor();
                v = op == '*' ? v * r : v / r;
                SkipSpace();
            }
            return v;
        }

        private double ParseFactor()
        {
            SkipSpace();
            if (Peek() == '(')
            {
                Eat();
                var v = ParseExpr();
                SkipSpace();
                if (Peek() != ')') throw new FormatException("缺少右括号");
                Eat();
                return v;
            }
            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipSpace();
            int start = _i;
            while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.')) _i++;
            if (start == _i) throw new FormatException($"无法解析的字符 '{Peek()}'");
            return double.Parse(_s.AsSpan(start, _i - start));
        }

        private char Peek() => _i < _s.Length ? _s[_i] : '\0';
        private char Eat() => _s[_i++];
        private void SkipSpace() { while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++; }
        public void ExpectEnd() => SkipSpace();
    }
}
