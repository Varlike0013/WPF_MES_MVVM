namespace WPF_MES.Shared;

/// <summary>
/// 重工页的查询条件类别
/// </summary>
public enum ConditionType
{
    /// <summary>序列号 (SN)</summary>
    SerialNumber,
    /// <summary>箱号 (CARTON)</summary>
    Carton,
    /// <summary>重工号 (REWORK)</summary>
    Rework,
    /// <summary>工单 (WONO)</summary>
    WorkOrder,
    /// <summary>抽验号 (QCNO)</summary>
    QcNo,
    /// <summary>二维码(Qrcode)</summary>
    QrCode,
    /// <summary>料件(KeyPart)</summary>
    KeyPart,
}
/// <summary>
/// 条件管理器（泛型版）。
/// key 用 enum 声明，value 是一组字符串。
/// </summary>
public class ConditionManager
{
    private readonly Dictionary<ConditionType, List<string>> _conditions = new();

    /// <summary>变化事件（Add / Remove / Clear 时触发）</summary>
    public event Action? Changed;

    /// <summary>
    /// 添加条件。value 为空时忽略，已存在则跳过。
    /// </summary>
    public void Add(ConditionType key, string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (_conditions.TryGetValue(key, out var list))
        {
            if (!list.Contains(value))
            {
                list.Add(value);
                Changed?.Invoke();
            }
        }
        else
        {
            _conditions[key] = new List<string> { value };
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// 批量添加。
    /// </summary>
    public void AddRange(ConditionType key, IEnumerable<string> values)
    {
        bool added = false;

        if (!_conditions.TryGetValue(key, out var list))
        {
            list = new List<string>();
            _conditions[key] = list;
        }

        foreach (var v in values)
        {
            if (string.IsNullOrEmpty(v)) continue;
            if (!list.Contains(v))
            {
                list.Add(v);
                added = true;
            }
        }

        if (added) Changed?.Invoke();
    }

    /// <summary>
    /// 删除某个 value。list 空了就连 key 一起删。
    /// </summary>
    /// <returns>是否删除了</returns>
    public bool Remove(ConditionType key, string value)
    {
        if (!_conditions.TryGetValue(key, out var list)) return false;

        bool removed = list.Remove(value);
        if (list.Count == 0)
            _conditions.Remove(key);

        if (removed) Changed?.Invoke();
        return removed;
    }

    /// <summary>清空全部</summary>
    public void Clear()
    {
        if (_conditions.Count == 0) return;
        _conditions.Clear();
        Changed?.Invoke();
    }

    /// <summary>取某个 key 的所有 value。不存在返回空列表</summary>
    public List<string> GetValues(ConditionType key)
        => _conditions.TryGetValue(key, out var list)
            ? new List<string>(list)
            : new List<string>();

    /// <summary>取全部（深拷贝）</summary>
    public Dictionary<ConditionType, List<string>> GetAll()
    {
        var result = new Dictionary<ConditionType, List<string>>();
        foreach (var kv in _conditions)
            result[kv.Key] = new List<string>(kv.Value);
        return result;
    }

    /// <summary>判断某 key/value 是否存在</summary>
    public bool Exists(ConditionType key, string value)
        => _conditions.TryGetValue(key, out var list) && list.Contains(value);

    /// <summary>某个 key 是否有值</summary>
    public bool HasAny(ConditionType key)
        => _conditions.TryGetValue(key, out var list) && list.Count > 0;

    /// <summary>当前条件类别数</summary>
    public int Count => _conditions.Count;

    /// <summary>是否为空</summary>
    public bool IsEmpty => _conditions.Count == 0;
}