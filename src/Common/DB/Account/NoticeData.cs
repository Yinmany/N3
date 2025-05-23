namespace ProjectX.DB;

/// <summary>
/// 登录前的公告
/// </summary>
public class NoticeData
{
    /// <summary>
    /// Id(只有一个默认为0)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; }
}