namespace WPF_MES.Contracts.Models
{
    /// <summary>
    /// 客户信息
    /// </summary>
    public class CustomerInfo
    {
        /// <summary>客户 ID (SYS_CUSTOMER.CUSTOMER_ID)</summary>
        public int CustomerId { get; set; }

        /// <summary>客户编码 (SYS_CUSTOMER.CUSTOMER_CODE)</summary>
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>客户名称 (SYS_CUSTOMER.CUSTOMER_NAME)</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>显示文本：编码 + 名称</summary>
        public string Display => string.IsNullOrEmpty(CustomerCode)
            ? CustomerName
            : $"{CustomerCode} - {CustomerName}";

        public override string ToString() => Display;
    }
}