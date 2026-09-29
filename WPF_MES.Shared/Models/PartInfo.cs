namespace WPF_MES.Contracts.Models
{
    /// <summary>
    /// 料号信息
    /// </summary>
    public class PartInfo
    {
        /// <summary>料号 ID (SYS_PART.PART_ID)</summary>
        public int PartId { get; set; }

        /// <summary>料号 (SYS_PART.PART_NO)</summary>
        public string PartNo { get; set; } = string.Empty;

        /// <summary>料号描述 (SYS_PART.SPEC1)</summary>
        public string PartDesc { get; set; } = string.Empty;

        public PartInfo() { }

        public PartInfo(int partId, string partNo, string partDesc)
        {
            PartId = partId;
            PartNo = partNo;
            PartDesc = partDesc;
        }

        public override string ToString() => PartNo;
    }
}