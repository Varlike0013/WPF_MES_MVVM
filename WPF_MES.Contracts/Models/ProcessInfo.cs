namespace WPF_MES.Contracts.Models
{
    /// <summary>
    /// 工序信息（ID + 名称）
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>工序 ID（SYS_PROCESS.PROCESS_ID）</summary>
        public int ProcessId { get; set; }

        /// <summary>工序名称（SYS_PROCESS.PROCESS_NAME）</summary>
        public string ProcessName { get; set; } = string.Empty;

        public ProcessInfo() { }

        public ProcessInfo(int processId, string processName)
        {
            ProcessId = processId;
            ProcessName = processName;
        }

        public override string ToString() => ProcessName;
    }
}