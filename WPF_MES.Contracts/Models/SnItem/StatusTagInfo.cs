namespace WPF_MES.Contracts.Models.SnItem
{
    /// <summary>
    /// 状态表顶部标签的数据
    /// </summary>
    public class StatusTagInfo
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;

        // ============ 料号 ============
        /// <summary>料号信息 (MODEL_ID)</summary>
        public PartInfo Part { get; set; } = new();

        // ============ 三个工序 ============
        public ProcessInfo Process { get; set; } = new();
        public ProcessInfo NextProcess { get; set; } = new();
        public ProcessInfo WipProcess { get; set; } = new();

        // ============ 其他 ============
        public string CustomerSN { get; set; } = string.Empty;
        public string CartonNo { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string SSN { get; set; } = string.Empty;
        public string PcbQrCode { get; set; } = string.Empty;
        public string ReworkNo { get; set; } = string.Empty;

        /// <summary>当前状态 (CURRENT_STATUS)，VARCHAR2(1)</summary>
        public string CurrentStatus { get; set; } = string.Empty;

        /// <summary>作业标志 (WORK_FLAG)，VARCHAR2(1)</summary>
        public string WorkFlag { get; set; } = string.Empty;

        // ============ 解析属性 ============
        public int CurrentStatusCode
            => int.TryParse(CurrentStatus, out var v) ? v : 0;

        public string CurrentStatusText => CurrentStatusCode switch
        {
            0 => "Good",
            1 => "Fail",
            2 => "Hold",
            _ => CurrentStatus,
        };

        public int WorkFlagCode
            => int.TryParse(WorkFlag, out var v) ? v : 0;

        public string WorkFlagText => WorkFlagCode switch
        {
            0 => "Good",
            1 => "Fail",
            _ => WorkFlag,
        };
    }
}