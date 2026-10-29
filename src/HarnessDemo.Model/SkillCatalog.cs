using System.Text.RegularExpressions;

namespace HarnessDemo.Model;

/// <summary>单个 Skill 的元数据（从 SKILL.md frontmatter 解析）。</summary>
public sealed record SkillDef(
    string Name,
    string Description,
    string Agent,
    string[] AllowedTools);

/// <summary>
/// 启动时扫描 skills/ 目录，解析每个 SKILL.md 的 frontmatter，
/// 构建 agent -> skills -> allowed-tools 的映射。
/// Agent 路由后，按当前 Agent 的 allowed-tools 过滤可用工具。
/// </summary>
public sealed class SkillCatalog
{
    private readonly Dictionary<string, List<SkillDef>> _byAgent = new();
    private readonly Dictionary<string, HashSet<string>> _toolsByAgent = new();

    public SkillCatalog(string skillsRoot)
    {
        Load(skillsRoot);
    }

    private void Load(string root)
    {
        if (!Directory.Exists(root)) return;
        foreach (var md in Directory.GetFiles(root, "SKILL.md", SearchOption.AllDirectories))
        {
            var skill = Parse(md);
            if (skill == null) continue;
            if (!_byAgent.TryGetValue(skill.Agent, out var list))
                _byAgent[skill.Agent] = list = new List<SkillDef>();
            list.Add(skill);

            if (!_toolsByAgent.TryGetValue(skill.Agent, out var tools))
                _toolsByAgent[skill.Agent] = tools = new HashSet<string>();
            foreach (var t in skill.AllowedTools) tools.Add(t);
        }
    }

    private SkillDef? Parse(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            var m = Regex.Match(text, @"^---\s*\n(.*?)\n---", RegexOptions.Singleline);
            if (!m.Success) return null;
            var fm = m.Groups[1].Value;

            string? name = null, desc = null;
            var tools = new List<string>();
            foreach (var line in fm.Split('\n'))
            {
                if (line.StartsWith("name:")) name = line.Substring(5).Trim().Trim('"');
                else if (line.StartsWith("description:")) desc = line.Substring(11).Trim().Trim('"');
                else if (line.StartsWith("allowed-tools:"))
                {
                    var arr = line.Substring(14).Trim().Trim('[', ']');
                    if (!string.IsNullOrWhiteSpace(arr))
                        tools.AddRange(arr.Split(',').Select(s => s.Trim().Trim('"')));
                }
            }
            // 从路径推断 agent：skills/<agent>/<skill>/SKILL.md
            var parts = path.Replace('\\', '/').Split('/');
            var idx = Array.IndexOf(parts, "skills");
            var agent = idx >= 0 && parts.Length > idx + 1 ? parts[idx + 1] : "intro";
            return new SkillDef(name ?? "", desc ?? "", agent, tools.ToArray());
        }
        catch { return null; }
    }

    public IReadOnlyList<SkillDef> GetSkillsFor(string agent) =>
        _byAgent.TryGetValue(agent, out var list) ? list : Array.Empty<SkillDef>();

    public IReadOnlySet<string> GetToolsFor(string agent) =>
        _toolsByAgent.TryGetValue(agent, out var tools) ? tools : new HashSet<string>();

    public IReadOnlyDictionary<string, List<SkillDef>> All => _byAgent;
}
