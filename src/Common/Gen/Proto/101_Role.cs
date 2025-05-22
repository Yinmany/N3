using ProtoBuf;
using N3Core;
using System.Collections.Generic;
using System;

namespace Ystx2
{
    
    /// <summary> 角色基础数据消息 </summary>
    [ProtoContract]
    public partial class G2C_Role_InfoMsg : IMessage
    {
        public const int _MsgId_ = (int)Ystx2.MsgId.G2C_Role_InfoMsg;
        public int MsgId => _MsgId_;
    
        /// <summary> 角色id </summary>
        [ProtoMember(1)] public int RoleId { get; set; }
        /// <summary> 角色名称 </summary>
        [ProtoMember(2)] public string Name { get; set; }
        /// <summary> 角色性别: 0无 1男 2女 </summary>
        [ProtoMember(3)] public int Sex { get; set; }
        /// <summary> 经验 </summary>
        [ProtoMember(4)] public int Exp { get; set; }
        /// <summary> 外观 </summary>
        [ProtoMember(5)] public PbRoleAppearance Appearance { get; set; }
    }
    
}