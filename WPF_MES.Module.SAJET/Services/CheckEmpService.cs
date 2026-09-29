using System.Data;
using WPF_MES.Module.SAJET.Models;
using WPF_MES.Shared;
using WPF_MES.Shared.Models;

namespace WPF_MES.Module.SAJET.Services;

/// <summary>
/// 员工管理相关操作
/// </summary>
internal static class CheckEmpService
{
    // ============ 查询员工列表 ============

    /// <summary>
    /// 按条件查询员工
    /// </summary>
    /// <param name="queryType">0=工号精确 1=名称模糊</param>
    public static List<EmpInfoItem> QueryEmployees(int queryType, string input)
    {
        string whereClause;
        var ps = new Dictionary<string, object>();

        if (queryType == 0)
        {
            whereClause = "E.EMP_NO = :input";
            ps["input"] = input;
        }
        else
        {
            whereClause = "E.EMP_NAME LIKE :input";
            ps["input"] = "%" + input + "%";
        }

        string sql = $@"
            SELECT E.EMP_NO, E.EMP_NAME, E.EMAIL,
                   TO_CHAR(E.UPDATE_TIME, 'YYYY/MM/DD HH24:MI:SS') AS UPDATE_TIME,
                   D.DEPT_NAME
            FROM SAJET.SYS_EMP E
            LEFT JOIN SAJET.SYS_DEPT D ON D.DEPT_ID = E.DEPT_ID
            WHERE {whereClause}
            ORDER BY E.UPDATE_TIME DESC";

        var dt = OracleHelper.QueryDataTable(sql, ps);
        var list = new List<EmpInfoItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new EmpInfoItem
            {
                EmpNo = OracleHelper.GetStr(r, "EMP_NO"),
                EmpName = OracleHelper.GetStr(r, "EMP_NAME"),
                Email = OracleHelper.GetStr(r, "EMAIL"),
                UpdateTime = OracleHelper.GetStr(r, "UPDATE_TIME"),
                DeptName = OracleHelper.GetStr(r, "DEPT_NAME"),
            });
        }
        return list;
    }

    // ============ 加载所有角色 ============

    public static List<RoleItem> GetAllRoles()
    {
        const string sql = @"
            SELECT ROLE_ID, ROLE_NAME, ROLE_DESC
            FROM SAJET.SYS_ROLE
            WHERE ENABLED = 'Y'
            ORDER BY ROLE_NAME";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<RoleItem>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new RoleItem
            {
                RoleId = OracleHelper.GetInt(r, "ROLE_ID"),
                RoleName = OracleHelper.GetStr(r, "ROLE_NAME"),
                RoleDesc = OracleHelper.GetStr(r, "ROLE_DESC"),
                IsChecked = false,
            });
        }
        return list;
    }

    // ============ 查某员工已有的角色 ID ============

    public static HashSet<int> GetEmployeeRoleIds(string empNo)
    {
        const string sql = @"
            SELECT R.ROLE_ID
            FROM SAJET.SYS_ROLE_EMP R
            WHERE R.EMP_ID = (SELECT EMP_ID FROM SAJET.SYS_EMP WHERE EMP_NO = :no)";

        var ps = new Dictionary<string, object> { { "no", empNo } };
        var dt = OracleHelper.QueryDataTable(sql, ps);

        var set = new HashSet<int>();
        foreach (DataRow r in dt.Rows)
        {
            if (r[0] != DBNull.Value)
                set.Add(Convert.ToInt32(r[0]));
        }
        return set;
    }

    /// <summary>
    /// 根据工号查 EMP_ID。不存在返回 0。
    /// </summary>
    public static int GetEmpIdByNo(string empNo)
    {
        const string sql = @"
            SELECT EMP_ID FROM SAJET.SYS_EMP
            WHERE EMP_NO = :no AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "no", empNo } };
        var result = OracleHelper.ExecuteScalar(sql, ps);

        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 判断员工是否存在
    /// </summary>
    public static bool ExistsEmployee(string empNo)
        => GetEmpIdByNo(empNo) > 0;

    // ============ 保存员工角色 ============

    /// <summary>
    /// 保存员工角色（增删差异部分）。
    /// 事务保证一致。
    /// </summary>
    /// <param name="empNo">员工工号</param>
    /// <param name="oldRoleIds">原角色 ID 集合</param>
    /// <param name="newRoleIds">新角色 ID 集合</param>
    /// <returns>受影响行数之和</returns>
    public static int SaveEmployeeRoles(
        string empNo,
        HashSet<int> oldRoleIds,
        HashSet<int> newRoleIds)
    {
        int empId = GetEmpIdByNo(empNo);
        if (empId == 0)
            throw new Exception($"未找到员工：{empNo}");

        var toAdd = new HashSet<int>(newRoleIds);
        toAdd.ExceptWith(oldRoleIds);

        var toRemove = new HashSet<int>(oldRoleIds);
        toRemove.ExceptWith(newRoleIds);

        if (toAdd.Count == 0 && toRemove.Count == 0) return 0;

        var statements = new List<(string, Dictionary<string, object>?)>();

        // 删除
        foreach (var roleId in toRemove)
        {
            statements.Add((
                "DELETE FROM SAJET.SYS_ROLE_EMP WHERE EMP_ID = :empId AND ROLE_ID = :roleId",
                new Dictionary<string, object>
                {
                    { "empId", empId },
                    { "roleId", roleId },
                }));
        }

        // 新增
        foreach (var roleId in toAdd)
        {
            statements.Add((
                "INSERT INTO SAJET.SYS_ROLE_EMP (EMP_ID, ROLE_ID) VALUES (:empId, :roleId)",
                new Dictionary<string, object>
                {
                    { "empId", empId },
                    { "roleId", roleId },
                }));
        }

        return OracleHelper.ExecuteInTransaction(statements);
    }

    // ============ 部门列表 ============

    public static List<DeptOption> GetAllDepts()
    {
        const string sql = @"
            SELECT DEPT_ID, DEPT_NAME
            FROM SAJET.SYS_DEPT
            ORDER BY DEPT_NAME";

        var dt = OracleHelper.QueryDataTable(sql);
        var list = new List<DeptOption>(dt.Rows.Count);

        foreach (DataRow r in dt.Rows)
        {
            list.Add(new DeptOption
            {
                DeptId = OracleHelper.GetInt(r, "DEPT_ID"),
                DeptName = OracleHelper.GetStr(r, "DEPT_NAME"),
            });
        }
        return list;
    }

    // ============ 保存员工信息 ============

    /// <summary>
    /// 保存员工信息（新增或更新）。
    /// </summary>
    public static int SaveEmployeeInfo(
        string empNo,
        string empName,
        string email,
        int deptId,
        string desc,
        bool isQuit,
        DateTime? quitDate)
    {
        bool exists = ExistsEmployee(empNo);

        var ps = new Dictionary<string, object>
        {
            { "empNo",   empNo },
            { "empName", empName },
            { "email",   string.IsNullOrEmpty(email) ? (object)DBNull.Value : email },
            { "deptId",  deptId },
            { "desc",    string.IsNullOrEmpty(desc) ? (object)DBNull.Value : desc },
            { "enabled", isQuit ? "N" : "Y" },
        };

        if (exists)
        {
            // 更新
            string sql = @"
                UPDATE SAJET.SYS_EMP
                SET EMP_NAME = :empName,
                    EMAIL    = :email,
                    DEPT_ID  = :deptId,
                    DESCRIPTION = :desc,
                    ENABLED  = :enabled,
                    UPDATE_TIME = SYSDATE";

            if (isQuit && quitDate.HasValue)
            {
                sql += ", QUIT_DATE = :quitDate";
                ps["quitDate"] = quitDate.Value;
            }

            sql += " WHERE EMP_NO = :empNo";

            return OracleHelper.ExecuteNonQuery(sql, ps);
        }
        else
        {
            // 新增
            if (isQuit)
                throw new Exception("新员工不能直接设置离职状态");

            string sql = @"
                INSERT INTO SAJET.SYS_EMP 
                    (EMP_NO, EMP_NAME, EMAIL, DEPT_ID, DESCRIPTION, ENABLED, UPDATE_TIME)
                VALUES 
                    (:empNo, :empName, :email, :deptId, :desc, 'Y', SYSDATE)";

            return OracleHelper.ExecuteNonQuery(sql, ps);
        }
    }

    // ============ 按工号取员工完整信息 ============

    public static (string EmpNo, string EmpName, string Email, int DeptId, string Desc, string Enabled, DateTime? QuitDate)?
        GetEmployeeDetail(string empNo)
    {
        const string sql = @"
            SELECT EMP_NO, EMP_NAME, EMAIL, DEPT_ID, TRAINING_LIST, ENABLED, QUIT_DATE
            FROM SAJET.SYS_EMP
            WHERE EMP_NO = :no AND ROWNUM = 1";

        var ps = new Dictionary<string, object> { { "no", empNo } };
        var dt = OracleHelper.QueryDataTable(sql, ps);
        if (dt.Rows.Count == 0) return null;

        var r = dt.Rows[0];
        return (
            OracleHelper.GetStr(r, "EMP_NO"),
            OracleHelper.GetStr(r, "EMP_NAME"),
            OracleHelper.GetStr(r, "EMAIL"),
            OracleHelper.GetInt(r, "DEPT_ID"),
            OracleHelper.GetStr(r, "TRAINING_LIST"),
            OracleHelper.GetStr(r, "ENABLED"),
            OracleHelper.GetDate(r, "QUIT_DATE")
        );
    }
}