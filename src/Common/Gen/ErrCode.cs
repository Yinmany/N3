
namespace N3
{
    public static partial class ErrCode
    {
        /// <summary> 
        /// 
        /// 
        /// </summary>
        public const int OK = 0;
        /// <summary> 
        /// 登录失败，用户不存在!
        /// <para>用户不存在!</para> 
        /// </summary>
        public const int LOGIN_USER_NO_EXISTS = 1000;
        /// <summary> 
        /// 登录失败，账号被禁用!
        /// 
        /// </summary>
        public const int LOGIN_USER_DISABLE = 1001;
        /// <summary> 
        /// 登录失败，用户名或密码错误
        /// 
        /// </summary>
        public const int LOGIN_FAIL_USERNAME_OR_PASSWORD = 1002;
        /// <summary> 
        /// 登录失败，无游戏服
        /// 
        /// </summary>
        public const int LOGIN_NO_GAME_SERVER = 1003;
        /// <summary> 
        /// 登录失败，请重试
        /// 
        /// </summary>
        public const int LOGIN_FAIL_RETRY = 1004;
        /// <summary> 
        /// 登录失败，在别处登录
        /// 
        /// </summary>
        public const int LOGIN_FAIL_OTHER_PLACE_LOGIN = 2000;
        /// <summary> 
        /// 登录失败，玩家数据不存在
        /// 
        /// </summary>
        public const int LOGIN_FAIL_PLAYER_NO_EXISTS = 2001;
        /// <summary> 
        /// 登录失败，token验证失败
        /// 
        /// </summary>
        public const int LOGIN_FAIL_TOKEN_VERIFY = 2002;

    }
}