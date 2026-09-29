namespace WPF_MES.Contracts.Models.SnItem
{
    public class SnStatus
    {
        public string WorkOrder { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        // ============ 料号 ============
        /// <summary>料号信息 (MODEL_ID)</summary>
        public PartInfo Part { get; set; } = new();

        // ============ 三个工序 ============
        public ProcessInfo Process { get; set; } = new();
        public ProcessInfo NextProcess { get; set; } = new();
        public ProcessInfo WipProcess { get; set; } = new();

        // ============ 其他字段 ============
        public int RouteId { get; set; }
        public int PdlineId { get; set; }
        public int StageId { get; set; }
        public int TerminalId { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string WorkFlag { get; set; } = string.Empty;
        public DateTime? UpdateTime { get; set; }
        public string PalletNo { get; set; } = string.Empty;
        public string CartonNo { get; set; } = string.Empty;
        public string Container { get; set; } = string.Empty;
        public string QcNo { get; set; } = string.Empty;
        public string QcResult { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string ReworkNo { get; set; } = string.Empty;
        public int EmpId { get; set; }
        public string CustomerSn { get; set; } = string.Empty;

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
            1 => "Scap",
            _ => WorkFlag,
        };
    }
}