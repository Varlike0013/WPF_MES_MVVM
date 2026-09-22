using System;
using System.Collections.Generic;
using System.Text;

namespace WPF_MES.Contracts.Models
{
    /// <summary>
    /// 状态表顶部 12 个标签的数据
    /// </summary>
    public class StatusTagInfo
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;
        public string PartNo { get; set; } = string.Empty;
        public string PartDesc { get; set; } = string.Empty;
        public string NextProcess { get; set; } = string.Empty;
        public int WorkFlag { get; set; }
        public string WorkFlagText { get; set; } = string.Empty;
        public string CustomerSN { get; set; } = string.Empty;
        public string CartonNo { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string SSN { get; set; } = string.Empty;
        public string PcbQrCode { get; set; } = string.Empty;
    }
}